using Foundation.Api.ApiControllers;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using Foundation.Model.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.OData.Builder;

namespace Foundation.Api.Middlewares.WebApi.OData.Implementations
{
    public class DefaultAutoEdmBuilder : IAutoEdmBuilder
    {
        private MethodInfo _buildControllerOperations = null;
        private MethodInfo _buildDto = null;
        private MethodInfo _collectionParamterMethodInfo = null;

        public DefaultAutoEdmBuilder()
        {
            _buildControllerOperations = GetType().GetTypeInfo().GetMethod(nameof(BuildControllerOperations), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _buildDto = GetType().GetTypeInfo().GetMethod(nameof(BuildDto), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            _collectionParamterMethodInfo = typeof(ActionConfiguration).GetTypeInfo().GetMethod(nameof(ActionConfiguration.CollectionParameter));
        }

        public virtual void AutoBuildEdmFromAssembly(Assembly assembly, ODataModelBuilder modelBuilder)
        {
            List<TypeInfo> controllers = assembly
                .GetTypes()
                .Select(t => t.GetTypeInfo())
                .Where(t =>
                {
                    TypeInfo baseGenericType = (t.BaseType?.GetTypeInfo()?.IsGenericType == true ? t.BaseType?.GetTypeInfo()?.GetGenericTypeDefinition() : null)?.GetTypeInfo();

                    while (baseGenericType != null)
                    {
                        if (baseGenericType == typeof(DtoController<>).GetTypeInfo() || baseGenericType == typeof(DtoSetController<,,>).GetTypeInfo())
                            return true;

                        baseGenericType = (baseGenericType.BaseType?.GetTypeInfo()?.IsGenericType == true ? baseGenericType.BaseType?.GetTypeInfo()?.GetGenericTypeDefinition() : null)?.GetTypeInfo();
                    }

                    return false;
                })
                .ToList();

            AutoBuildEdmFromTypes(controllers, modelBuilder);
        }

        public virtual void AutoBuildEdmFromTypes(IEnumerable<TypeInfo> controllers, ODataModelBuilder modelBuilder)
        {
            foreach (TypeInfo controller in controllers)
            {
                TypeInfo dtoType = controller.BaseType.GetGenericArguments().Single(t => t.GetInterfaces().Any(i => i.Name == nameof(IDto))).GetTypeInfo();
                _buildDto.MakeGenericMethod(dtoType).Invoke(this, new object[] { modelBuilder, controller });
            }

            foreach (TypeInfo controller in controllers)
            {
                TypeInfo dtoType = controller.BaseType.GetGenericArguments().Single(t => t.GetInterfaces().Any(i => i.Name == nameof(IDto))).GetTypeInfo();
                _buildControllerOperations.MakeGenericMethod(dtoType).Invoke(this, new object[] { modelBuilder, controller });
            }
        }

        private void BuildDto<TDto>(ODataModelBuilder modelBuilder, TypeInfo apiController)
             where TDto : class
        {
            TypeInfo dtoType = typeof(TDto).GetTypeInfo();
            string controllerName = apiController.Name.Replace("Controller", string.Empty);
            EntitySetConfiguration<TDto> entitySet = modelBuilder.EntitySet<TDto>(controllerName);
            entitySet.EntityType.DerivesFromNothing();
        }

        private void BuildControllerOperations<TDto>(ODataModelBuilder modelBuilder, TypeInfo apiController)
            where TDto : class
        {
            TypeInfo dtoType = typeof(TDto).GetTypeInfo();
            string controllerName = apiController.Name.Replace("Controller", string.Empty);
            EntitySetConfiguration<TDto> entitySet = modelBuilder.EntitySet<TDto>(controllerName);

            foreach (MethodInfo method in apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                IActionHttpMethodProvider actionHttpMethodProvider =
                    method.GetCustomAttributes().OfType<FunctionAttribute>().Cast<IActionHttpMethodProvider>()
                    .Union(method.GetCustomAttributes().OfType<ActionAttribute>().Cast<IActionHttpMethodProvider>())
                    .SingleOrDefault();

                if (actionHttpMethodProvider != null)
                {
                    bool isFunction = actionHttpMethodProvider is FunctionAttribute;
                    bool isAction = actionHttpMethodProvider is ActionAttribute;

                    if (!isFunction && !isAction)
                        continue;

                    List<ParameterAttribute> actionParameters =
                        method.GetCustomAttributes<ParameterAttribute>().ToList();

                    OperationConfiguration operationConfiguration = null;

                    if (isAction)
                        operationConfiguration = entitySet.EntityType.Collection.Action(method.Name);
                    else if (isFunction)
                        operationConfiguration = entitySet.EntityType.Collection.Function(method.Name);

                    foreach (ParameterAttribute actionParameter in actionParameters)
                    {
                        if (actionParameter.Type.GetTypeInfo() != typeof(string).GetTypeInfo() &&
                            typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(actionParameter.Type.GetTypeInfo()))
                        {
                            ParameterConfiguration parameter =
                                (ParameterConfiguration)
                                    _collectionParamterMethodInfo.MakeGenericMethod(
                                        actionParameter.Type.GetGenericArguments().Single())
                                        .Invoke(operationConfiguration, new object[] { actionParameter.Name });

                            parameter.OptionalParameter = actionParameter.IsOptional;
                        }
                        else
                        {
                            operationConfiguration.Parameter(actionParameter.Type, actionParameter.Name)
                                .OptionalParameter =
                                actionParameter.IsOptional;
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
                                type = type.GetGenericArguments().Single().GetTypeInfo();
                        }

                        if (typeof(string) != type && typeof(IEnumerable).IsAssignableFrom(type))
                        {
                            if (type.IsGenericType)
                                type = type.GetGenericArguments().Single().GetTypeInfo();
                            isCollection = true;
                        }

                        if (type.GetInterface(nameof(IDto)) != null)
                        {
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
                            if (isAction)
                                ((ActionConfiguration)operationConfiguration).Returns(type);
                            else if (isFunction)
                                ((FunctionConfiguration)operationConfiguration).Returns(type);
                        }
                    }
                    else
                    {
                        if (isFunction)
                            throw new InvalidOperationException("Function must have a return type, use action instead");

                        operationConfiguration.OptionalReturn = true;
                    }
                }
            }
        }
    }
}
