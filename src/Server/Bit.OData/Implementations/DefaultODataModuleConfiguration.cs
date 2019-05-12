using Bit.Model.Implementations;
using Bit.OData.Contracts;
using Bit.OData.ODataControllers;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

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
    public class DefaultAutoODataModelBuilderParameterInfo
    {
        public string Name { get; set; }

        public TypeInfo Type { get; set; }

        public bool IsOptional => Type.IsClass || Nullable.GetUnderlyingType(Type) != null;
    }

    public class DefaultODataModuleConfiguration : IODataModuleConfiguration
    {
        private readonly MethodInfo _buildControllerOperations;
        private readonly MethodInfo _buildDto;
        private readonly MethodInfo _collectionParameterMethodInfo;

        public DefaultODataModuleConfiguration()
        {
            _buildControllerOperations = GetType().GetTypeInfo().GetMethod(nameof(BuildControllerOperations), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _buildDto = GetType().GetTypeInfo().GetMethod(nameof(BuildDto), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _collectionParameterMethodInfo = typeof(ActionConfiguration).GetTypeInfo().GetMethod(nameof(ActionConfiguration.CollectionParameter));
        }

        public virtual void ConfigureODataModule(string odataRouteName, Assembly odataAssembly, ODataModelBuilder odataModelBuilder)
        {
            List<TypeInfo> controllers = odataAssembly
                .GetLoadableExportedTypes()
                .Where(t => t.IsDtoController())
                .ToList();

            ConfigureODataModule(controllers, odataModelBuilder);
        }

        protected virtual void ConfigureODataModule(IEnumerable<TypeInfo> controllers, ODataModelBuilder odataModelBuilder)
        {
            var controllersWithDto = controllers
                .Select(c => new
                {
                    DtoType = DtoMetadataWorkspace.Current.GetFinalDtoType(c.BaseType?.GetGenericArguments().ExtendedSingle($"Finding dto in {c.Name}", t => DtoMetadataWorkspace.Current.IsDto(t.GetTypeInfo())).GetTypeInfo()),
                    Controller = c
                })
                .Where(c => c.DtoType != null)
                .ToList();

            foreach (var controllerWithDto in controllersWithDto)
            {
                _buildDto.MakeGenericMethod(controllerWithDto.DtoType).Invoke(this, new object[] { odataModelBuilder, controllerWithDto.Controller });
            }

            foreach (var controllerWithDto in controllersWithDto)
            {
                if (controllerWithDto.Controller.IsGenericType)
                    continue;
                _buildControllerOperations.MakeGenericMethod(controllerWithDto.DtoType).Invoke(this, new object[] { odataModelBuilder, controllerWithDto.Controller });
            }
        }

        private void BuildDto<TDto>(ODataModelBuilder odataModelBuilder, TypeInfo apiController)
             where TDto : class
        {
            TypeInfo dtoType = typeof(TDto).GetTypeInfo();
            string controllerName = GetControllerName(apiController);
            EntitySetConfiguration<TDto> entitySet = odataModelBuilder.EntitySet<TDto>(controllerName);
            if (GetBaseType(dtoType) == null)
                entitySet.EntityType.DerivesFromNothing();
        }

        private bool IsIEnumerable(TypeInfo type)
        {
            return type != typeof(string).GetTypeInfo() && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type);
        }

        private void BuildControllerOperations<TDto>(ODataModelBuilder odataModelBuilder, TypeInfo apiController)
            where TDto : class
        {
            string controllerName = GetControllerName(apiController);
            EntitySetConfiguration<TDto> entitySet = odataModelBuilder.EntitySet<TDto>(controllerName);

            foreach (MethodInfo method in apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                IActionHttpMethodProvider actionHttpMethodProvider =
                    method.GetCustomAttributes().OfType<IActionHttpMethodProvider>()
                    .ExtendedSingleOrDefault($"Finding ${nameof(IActionHttpMethodProvider)} attribute in {method.Name}");

                if (actionHttpMethodProvider is DeleteAttribute || actionHttpMethodProvider is UpdateAttribute || actionHttpMethodProvider is PartialUpdateAttribute)
                {
                    if (!method.GetParameters().Any(p => p.Name == "key"))
                    {
                        string dtoTypeName = typeof(TDto).Name;
                        string keyColumnTypeName = DtoMetadataWorkspace.Current.GetKeyColums(typeof(TDto).GetTypeInfo()).ExtendedSingle($"Getting key columns for {dtoTypeName}").PropertyType.Name;
                        string methodDeclartion = null;
                        if (actionHttpMethodProvider is DeleteAttribute)
                            methodDeclartion = $"public virtual async Task Delete({keyColumnTypeName} key, CancellationToken cancellationToken)";
                        else if (actionHttpMethodProvider is PartialUpdateAttribute)
                            methodDeclartion = $"public virtual async Task<{dtoTypeName}> PartialUpdate({keyColumnTypeName} key, Delta<{dtoTypeName}>, CancellationToken cancellationToken)";
                        else if (actionHttpMethodProvider is UpdateAttribute)
                            methodDeclartion = $"public virtual async Task<{dtoTypeName}> Update({keyColumnTypeName} key, {dtoTypeName}, CancellationToken cancellationToken)";
                        throw new InvalidOperationException($"{apiController.Name}.{method.Name} must have a signature 'like' followings: {methodDeclartion}");
                    }
                }

                if (actionHttpMethodProvider != null)
                {
                    bool isFunction = actionHttpMethodProvider is FunctionAttribute;
                    bool isAction = actionHttpMethodProvider is ActionAttribute;

                    TypeInfo returnType = method.ReturnType.GetTypeInfo();

                    if (typeof(Task).GetTypeInfo().IsAssignableFrom(returnType))
                    {
                        if (returnType.IsGenericType)
                            returnType = returnType.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                    }

                    if (DtoMetadataWorkspace.Current.IsDto(returnType))
                        throw new InvalidOperationException($"Use SingleResult<{returnType.Name}> to return one {returnType.Name} in {apiController.Name}.{method.Name}");

                    if (!isFunction && !isAction)
                        continue;

                    List<DefaultAutoODataModelBuilderParameterInfo> operationParameters = new List<DefaultAutoODataModelBuilderParameterInfo>();

                    if (isFunction)
                    {
                        foreach (ParameterInfo parameter in method.GetParameters())
                        {
                            TypeInfo parameterType = parameter.ParameterType.GetTypeInfo();
                            if (parameterType == typeof(CancellationToken).GetTypeInfo() || typeof(ODataQueryOptions).IsAssignableFrom(parameterType))
                                continue;
                            if (DtoMetadataWorkspace.Current.IsDto(parameterType) || DtoMetadataWorkspace.Current.IsComplexType(parameterType) || IsIEnumerable(parameterType))
                            {
                                // some types which are known to be problematic in functions if you accept them in function parameters. This types list is not complete for sure!
                                throw new InvalidOperationException($"Parameter {parameter.Name} of type {parameter.ParameterType.Name} is not allowed in {apiController.Name}.{method.Name} function.");
                            }
                            operationParameters.Add(new DefaultAutoODataModelBuilderParameterInfo { Name = parameter.Name, Type = parameterType });
                        }
                    }
                    else if (isAction)
                    {
                        ParameterInfo parameter = method
                            .GetParameters()
                            .ExtendedSingleOrDefault($"Finding parameter of {apiController.Name}.{method.Name}. It's expected to see 0 or 1 parameter only.", p => p.ParameterType.GetTypeInfo() != typeof(CancellationToken).GetTypeInfo() && !typeof(ODataQueryOptions).IsAssignableFrom(p.ParameterType.GetTypeInfo()));

                        if (parameter != null)
                        {
                            TypeInfo parameterType = parameter.ParameterType.GetTypeInfo();

                            if (DtoMetadataWorkspace.Current.IsDto(parameterType) || DtoMetadataWorkspace.Current.IsComplexType(parameterType) || IsIEnumerable(parameterType))
                            {
                                operationParameters.Add(new DefaultAutoODataModelBuilderParameterInfo { Name = parameter.Name, Type = parameterType });
                            }
                            else if (Nullable.GetUnderlyingType(parameterType) != null || parameterType.IsPrimitive || typeof(string).GetTypeInfo() == parameterType || parameter.ParameterType == typeof(DateTime).GetTypeInfo() || parameter.ParameterType == typeof(DateTimeOffset).GetTypeInfo() || parameter.ParameterType.IsEnum)
                            {
                                // some types which are known to be problematic in actions if you accept them in action parameters directly without any container class. This types list is not complete for sure!
                                throw new InvalidOperationException($"Allowed parameter types for {apiController.Name}.{method.Name} action: | Dto | Complex Type | Classes like pulic class {method.Name}Args {{ public {parameter.ParameterType.Name} {parameter.Name} {{ get; set; }} }} | IEnumerable<T> (For example IEnumerable<int> or IEnumerable<MyDtoClass> | You may not define a parameter of type {parameter.ParameterType.Name}.");
                            }
                            else
                            {
                                foreach (PropertyInfo prop in parameter.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                    operationParameters.Add(new DefaultAutoODataModelBuilderParameterInfo { Name = prop.Name, Type = prop.PropertyType.GetTypeInfo() });
                            }
                        }
                    }

                    OperationConfiguration operationConfiguration = null;

                    if (isAction)
                        operationConfiguration = entitySet.EntityType.Collection.Action(method.Name);
                    else if (isFunction)
                        operationConfiguration = entitySet.EntityType.Collection.Function(method.Name);

                    foreach (DefaultAutoODataModelBuilderParameterInfo operationParameter in operationParameters)
                    {
                        TypeInfo parameterType = operationParameter.Type;

                        if (IsIEnumerable(operationParameter.Type))
                        {
                            if (parameterType.IsArray)
                                throw new InvalidOperationException($"Use IEnumerable<{parameterType.GetElementType().GetTypeInfo().Name}> instead of {parameterType.GetElementType().GetTypeInfo().Name}[] for parameter {operationParameter.Name} of {operationParameter.Name} in {controllerName} controller");

                            if (parameterType.IsGenericType)
                                parameterType = parameterType.GetGenericArguments().ExtendedSingle($"Finding parameter type from generic arguments of {parameterType.Name}").GetTypeInfo();

                            ParameterConfiguration parameter = (ParameterConfiguration)_collectionParameterMethodInfo
                                                                                            .MakeGenericMethod(parameterType)
                                                                                            .Invoke(operationConfiguration, new object[] { operationParameter.Name });
                            parameter.Nullable = operationParameter.IsOptional;
                        }
                        else
                        {
                            operationConfiguration.Parameter(parameterType, operationParameter.Name).Nullable = operationParameter.IsOptional;
                        }
                    }

                    if (returnType.Name != "Void" && returnType.Name != typeof(Task).GetTypeInfo().Name)
                    {
                        operationConfiguration.ReturnNullable = false;

                        if (typeof(SingleResult).GetTypeInfo().IsAssignableFrom(returnType))
                        {
                            if (returnType.IsGenericType)
                                returnType = returnType.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                        }

                        bool isCollection = false;

                        if (IsIEnumerable(returnType))
                        {
                            if (returnType.IsGenericType)
                                returnType = returnType.GetGenericArguments().ExtendedSingle($"Finding Return type of {method.Name}").GetTypeInfo();
                            else if (returnType.IsArray)
                                returnType = returnType.GetElementType().GetTypeInfo();
                            isCollection = true;
                        }

                        if (DtoMetadataWorkspace.Current.IsDto(returnType))
                        {
                            returnType = DtoMetadataWorkspace.Current.GetFinalDtoType(returnType);

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
                                    ((ActionConfiguration)operationConfiguration).Returns(returnType);
                                else if (isFunction)
                                    ((FunctionConfiguration)operationConfiguration).Returns(returnType);
                            }
                            else
                            {
                                operationConfiguration.GetType()
                                    .GetTypeInfo()
                                    .GetMethod("ReturnsCollection")
                                    .MakeGenericMethod(returnType)
                                    .Invoke(operationConfiguration, Array.Empty<object>());
                            }
                        }
                    }
                    else
                    {
                        if (isFunction)
                            throw new InvalidOperationException($"Function {method.Name} in {apiController.Name} must have a return type, use action instead");

                        operationConfiguration.ReturnNullable = true;
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
            return (index == -1 ? name : name.Substring(0, index)).Replace("Controller", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
