using System;
using System.Linq;
using System.Reflection;

namespace Documentation
{
    public class Specifier<T> : ISpecifier
    {
        public string GetApiDescription()
        {
            var attributes =  typeof(T).GetCustomAttributes().OfType<ApiDescriptionAttribute>();
            if (attributes.Count() > 0)
                return attributes.First().Description;
            else return null;
        }

        public string[] GetApiMethodNames()
        {
            return typeof(T).GetMethods()
                .Where(m => m.GetCustomAttribute<ApiMethodAttribute>() != null)
                .Select(m => m.Name)
                .ToArray();
        }

        public string GetApiMethodDescription(string methodName)
        {
            var method = typeof(T).GetMethod(methodName);
            return method?.GetCustomAttribute<ApiDescriptionAttribute>()?.Description;
        }

        public string[] GetApiMethodParamNames(string methodName)
        {
            var method = typeof(T).GetMethod(methodName);
            if (method?.GetCustomAttribute<ApiDescriptionAttribute>() == null)
                throw new Exception(methodName + "is not an ApiMethod");

            return method.GetParameters()
                .Select(p => p.Name)
                .ToArray();
        }

        public string GetApiMethodParamDescription(string methodName, string paramName)
        {
            var method = typeof(T).GetMethod(methodName);
            var parameters = method?.GetParameters()
                .Where(p => p.Name == paramName);
            
            if (parameters == null  || parameters.Count() == 0)
                return null;

            return parameters.First()
                .GetCustomAttribute<ApiDescriptionAttribute>()?.Description;
        }

        public ApiParamDescription GetApiMethodParamFullDescription(string methodName, string paramName)
        {
            var result = new ApiParamDescription();
            var method = typeof(T).GetMethod(methodName);
            var parameter = GetParameter(method, paramName);

            CheckValidationAndRequiredAttributes(parameter, result);

            result.ParamDescription = new CommonDescription(paramName, 
                parameter?.GetCustomAttribute<ApiDescriptionAttribute>()?.Description);

            return result;
        }

        private ParameterInfo GetParameter(MethodInfo method, string paramName)
        {
            ParameterInfo parameter = null;
            var parameters = method?.GetParameters()
                .Where(p => p.Name == paramName);
            if (parameters != null && parameters.Count() > 0)
                parameter = parameters.First();

            return parameter;
        }

        public ApiMethodDescription GetApiMethodFullDescription(string methodName)
        {
            var result = new ApiMethodDescription();
            var method = typeof(T).GetMethod(methodName);
            if (method == null || method.GetCustomAttribute<ApiMethodAttribute>() == null)
                return null;

            result.MethodDescription = new CommonDescription()
            { 
                Name = method.Name,
                Description = method.GetCustomAttribute<ApiDescriptionAttribute>().Description
            };

            result.ParamDescriptions = method.GetParameters()
                .Select(p => GetApiMethodParamFullDescription(methodName, p.Name))
                .ToArray();

            result.ReturnDescription = GetReturnParameterOfMethodDescription(method);
            return result;
        }

        private ApiParamDescription GetReturnParameterOfMethodDescription(MethodInfo method)
        {
            var result = new ApiParamDescription();
            var parameter = method.ReturnParameter;
            if (parameter.ParameterType == typeof(void))
                return null;

            CheckValidationAndRequiredAttributes(parameter, result);
            return result;
        }

        private void CheckValidationAndRequiredAttributes(ParameterInfo parameter, ApiParamDescription result)
        {
            var apiValidationAttribute = parameter?.GetCustomAttribute<ApiIntValidationAttribute>();
            if (apiValidationAttribute != null)
            {
                result.MinValue = apiValidationAttribute.MinValue;
                result.MaxValue = apiValidationAttribute.MaxValue;
            }

            var apiRequiredAttribute = parameter?.GetCustomAttribute<ApiRequiredAttribute>();
            if (apiRequiredAttribute != null)
                result.Required = apiRequiredAttribute.Required;
        }
    }
}