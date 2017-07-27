using Bit.Model.Implementations;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Builder;
using System.Web.OData.Query;

namespace System.Reflection
{
    public static class TypeInfoExtensions
    {
        public static bool IsDtoController(this TypeInfo controllerType)
        {
            TypeInfo baseGenericType = (controllerType.BaseType?.GetTypeInfo()?.IsGenericType == true ? controllerType.BaseType?.GetTypeInfo()?.GetGenericTypeDefinition() : null)?.GetTypeInfo();

            while (baseGenericType != null)
            {
                if (typeof(DtoController<>).GetTypeInfo().IsAssignableFrom(baseGenericType))
                    return true;

                baseGenericType = (baseGenericType.BaseType?.GetTypeInfo()?.IsGenericType == true ? baseGenericType.BaseType?.GetTypeInfo()?.GetGenericTypeDefinition() : null)?.GetTypeInfo();
            }

            return false;
        }
    }
}

namespace Bit.OData.Implementations
{
    public class DefaultAutoEdmBuilderParameterInfo
    {
        public string Name { get; set; }

        public TypeInfo Type { get; set; }

        public bool IsOptional => Type.IsClass || Nullable.GetUnderlyingType(Type) != null;
    }

    public class DefaultAutoEdmBuilder : IAutoEdmBuilder
    {
        private readonly MethodInfo _buildControllerOperations = null;
        private readonly MethodInfo _buildDto = null;
        private readonly MethodInfo _collectionParameterMethodInfo = null;

        public DefaultAutoEdmBuilder()
        {
            _buildControllerOperations = GetType().GetTypeInfo().GetMethod(nameof(BuildControllerOperations), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _buildDto = GetType().GetTypeInfo().GetMethod(nameof(BuildDto), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _collectionParameterMethodInfo = typeof(ActionConfiguration).GetTypeInfo().GetMethod(nameof(ActionConfiguration.CollectionParameter));
        }

        public virtual void AutoBuildEdmFromAssembly(Assembly assembly, ODataModelBuilder modelBuilder)
        {
            List<TypeInfo> controllers = assembly
                .GetLoadableExportedTypes()
                .Where(t => t.IsDtoController())
                .ToList();

            AutoBuildEdmFromTypes(controllers, modelBuilder);
        }

        public virtual void AutoBuildEdmFromTypes(IEnumerable<TypeInfo> controllers, ODataModelBuilder modelBuilder)
        {
            var controllersWithDto = controllers
                .Select(c => new
                {
                    DtoType = DtoMetadataWorkspace.Current.GetFinalDtoType(c.BaseType?.GetGenericArguments().ExtendedSingleOrDefault($"Finding dto in {c.Name}", t => DtoMetadataWorkspace.Current.IsDto(t.GetTypeInfo())).GetTypeInfo()),
                    Controller = c
                })
                .Where(c => c.DtoType != null)
                .ToList();

            foreach (var controllerWithDto in controllersWithDto)
            {
                _buildDto.MakeGenericMethod(controllerWithDto.DtoType).Invoke(this, new object[] { modelBuilder, controllerWithDto.Controller });
            }

            foreach (var controllerWithDto in controllersWithDto)
            {
                if (controllerWithDto.Controller.IsGenericType)
                    continue;
                _buildControllerOperations.MakeGenericMethod(controllerWithDto.DtoType).Invoke(this, new object[] { modelBuilder, controllerWithDto.Controller });
            }
        }

        private void BuildDto<TDto>(ODataModelBuilder modelBuilder, TypeInfo apiController)
             where TDto : class
        {
            TypeInfo dtoType = typeof(TDto).GetTypeInfo();
            string controllerName = GetControllerName(apiController);
            EntitySetConfiguration<TDto> entitySet = modelBuilder.EntitySet<TDto>(controllerName);
            if (GetBaseType(dtoType) == null)
                entitySet.EntityType.DerivesFromNothing();
        }

        private void BuildControllerOperations<TDto>(ODataModelBuilder modelBuilder, TypeInfo apiController)
            where TDto : class
        {
            string controllerName = GetControllerName(apiController);
            EntitySetConfiguration<TDto> entitySet = modelBuilder.EntitySet<TDto>(controllerName);

            foreach (MethodInfo method in apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                IActionHttpMethodProvider actionHttpMethodProvider =
                    method.GetCustomAttributes().OfType<FunctionAttribute>().Cast<IActionHttpMethodProvider>()
                    .Union(method.GetCustomAttributes().OfType<ActionAttribute>().Cast<IActionHttpMethodProvider>())
                    .ExtendedSingleOrDefault($"Finding ${nameof(IActionHttpMethodProvider)} attribute in {method.Name}");

                if (actionHttpMethodProvider != null)
                {
                    bool isFunction = actionHttpMethodProvider is FunctionAttribute;
                    bool isAction = actionHttpMethodProvider is ActionAttribute;

                    if (!isFunction && !isAction)
                        continue;

                    List<DefaultAutoEdmBuilderParameterInfo> operationParameters = new List<DefaultAutoEdmBuilderParameterInfo>();

                    if (isFunction)
                    {
                        foreach (ParameterInfo parameter in method.GetParameters())
                        {
                            if (parameter.ParameterType.GetTypeInfo() == typeof(CancellationToken).GetTypeInfo() || typeof(ODataQueryOptions).IsAssignableFrom(parameter.ParameterType.GetTypeInfo()))
                                continue;
                            operationParameters.Add(new DefaultAutoEdmBuilderParameterInfo { Name = parameter.Name, Type = parameter.ParameterType.GetTypeInfo() });
                        }
                    }
                    else if (isAction)
                    {
                        ParameterInfo parameter = method
                            .GetParameters()
                            .ExtendedSingleOrDefault($"Finding parameter of {method.Name}", p => p.ParameterType.GetTypeInfo() != typeof(CancellationToken).GetTypeInfo() && !typeof(ODataQueryOptions).IsAssignableFrom(p.ParameterType.GetTypeInfo()));

                        if (parameter != null)
                        {
                            foreach (PropertyInfo prop in parameter.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                operationParameters.Add(new DefaultAutoEdmBuilderParameterInfo { Name = prop.Name, Type = prop.PropertyType.GetTypeInfo() });
                        }
                    }

                    OperationConfiguration operationConfiguration = null;

                    if (isAction)
                        operationConfiguration = entitySet.EntityType.Collection.Action(method.Name);
                    else if (isFunction)
                        operationConfiguration = entitySet.EntityType.Collection.Function(method.Name);

                    foreach (DefaultAutoEdmBuilderParameterInfo operationParameter in operationParameters)
                    {
                        TypeInfo parameterType = operationParameter.Type;

                        if (operationParameter.Type.GetTypeInfo() != typeof(string).GetTypeInfo() &&
                            typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(operationParameter.Type))
                        {
                            if (parameterType.IsArray)
                                throw new InvalidOperationException($"Use IEnumerable<{parameterType.GetElementType().GetTypeInfo().Name}> instead of {parameterType.GetElementType().GetTypeInfo().Name}[] for parameter {operationParameter.Name} of {operationParameter.Name} in {controllerName} controller");

                            if (parameterType.IsGenericType)
                                parameterType = parameterType.GetGenericArguments().ExtendedSingle($"Finding parameter type from generic arguments of {parameterType.Name}").GetTypeInfo();

                            ParameterConfiguration parameter = (ParameterConfiguration)_collectionParameterMethodInfo
                                                                                            .MakeGenericMethod(parameterType)
                                                                                            .Invoke(operationConfiguration, new object[] { operationParameter.Name });

                            parameter.OptionalParameter = operationParameter.IsOptional;
                        }
                        else
                        {
                            operationConfiguration.Parameter(parameterType, operationParameter.Name).OptionalParameter = operationParameter.IsOptional;
                        }
                    }

                    TypeInfo type = method.ReturnType.GetTypeInfo();

                    if (type.Name != "Void" && type.Name != typeof(Task).GetTypeInfo().Name)
                    {
                        operationConfiguration.OptionalReturn = false;

                        bool isCollection = false;

                        if (typeof(Task).GetTypeInfo().IsAssignableFrom(type))
                        {
                            if (type.IsGenericType)
                                type = type.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                        }

                        if (typeof(SingleResult).GetTypeInfo().IsAssignableFrom(type))
                        {
                            if (type.IsGenericType)
                                type = type.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                        }

                        if (typeof(string) != type && typeof(IEnumerable).IsAssignableFrom(type))
                        {
                            if (type.IsGenericType)
                                type = type.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                            else if (type.IsArray)
                                type = type.GetElementType().GetTypeInfo();
                            isCollection = true;
                        }

                        if (DtoMetadataWorkspace.Current.IsDto(type))
                        {
                            type = DtoMetadataWorkspace.Current.GetFinalDtoType(type);

                            if (isCollection == true)
                            {
                                if (isAction)
                                    ((ActionConfiguration)operationConfiguration).ReturnsCollectionFromEntitySet<TDto>(controllerName);
                                else
                                    ((FunctionConfiguration)operationConfiguration).ReturnsCollectionFromEntitySet<TDto>(controllerName);
                            }
                            else
                            {
                                if (isAction)
                                    ((ActionConfiguration)operationConfiguration).ReturnsFromEntitySet<TDto>(controllerName);
                                else if (isFunction)
                                    ((FunctionConfiguration)operationConfiguration).ReturnsFromEntitySet<TDto>(controllerName);
                            }
                        }
                        else
                        {
                            if (isCollection == false)
                            {
                                if (isAction)
                                    ((ActionConfiguration)operationConfiguration).Returns(type);
                                else if (isFunction)
                                    ((FunctionConfiguration)operationConfiguration).Returns(type);
                            }
                            else
                            {
                                operationConfiguration.GetType()
                                    .GetTypeInfo()
                                    .GetMethod("ReturnsCollection")
                                    .MakeGenericMethod(type)
                                    .Invoke(operationConfiguration, new object[] { });
                            }
                        }
                    }
                    else
                    {
                        if (isFunction)
                            throw new InvalidOperationException($"Function {method.Name} in {apiController.Name} must have a return type, use action instead");

                        operationConfiguration.OptionalReturn = true;
                    }
                }
            }
        }

        public virtual TypeInfo GetBaseType(TypeInfo dtoType)
        {
            if (dtoType == null)
                throw new ArgumentNullException(nameof(dtoType));

            if (DtoMetadataWorkspace.Current.IsDto(dtoType.BaseType.GetTypeInfo()))
                return dtoType.BaseType.GetTypeInfo();
            else
                return null;
        }

        public virtual string GetControllerName(TypeInfo type)
        {
            string name = type.Name;
            int index = name.IndexOf('`');
            return (index == -1 ? name : name.Substring(0, index)).Replace("Controller", string.Empty);
        }
    }
}
