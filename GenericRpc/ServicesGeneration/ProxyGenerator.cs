using GenericRpc.Exceptions;
using GenericRpc.Mediators;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GenericRpc.ServicesGeneration
{
    internal static class ProxyGenerator
    {
        private static object _assemblyInitLock = new object();
        private static ModuleBuilder _moduleBuilder;

        public static object ActivateProxyInstance(Type proxyType, IMediator mediator, ClientContext context)
            => Activator.CreateInstance(proxyType, new object[] { mediator, context });

        public static Type GenerateProxyType(Type interfaceType)
        {
            if (!interfaceType.IsVisible) throw new GenericRpcException("Interface type should be public");

            try
            {
                return GenerateProxyTypeInternal(interfaceType);
            }
            catch (Exception exception)
            {
                throw new GenericRpcException("Generating proxy service type error", exception);
            }
        }

        private static Type GenerateProxyTypeInternal(Type interfaceType)
        {
            // Generate module.
            var moduleBuilder = GetOrCreateModule();

            // Generate type.
            var generatedTypeName = $"{interfaceType.Name.Substring(1)}_{Guid.NewGuid():N}";
            var typeBuilder = moduleBuilder.DefineType(generatedTypeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, typeof(ProxyService));
            typeBuilder.AddInterfaceImplementation(interfaceType);

            // Generate constructor.
            var ctrArgumentsTypes = new Type[] { typeof(IMediator), typeof(ClientContext) };
            var ctrBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ctrArgumentsTypes);
            var ctrIlGenerator = ctrBuilder.GetILGenerator();
            ctrIlGenerator.Emit(OpCodes.Ldarg_0);
            ctrIlGenerator.Emit(OpCodes.Ldarg_1);
            ctrIlGenerator.Emit(OpCodes.Ldarg_2);
            var baseConstructor = typeof(ProxyService).GetConstructor(ctrArgumentsTypes);
            ctrIlGenerator.Emit(OpCodes.Callvirt, baseConstructor);
            ctrIlGenerator.Emit(OpCodes.Nop);
            ctrIlGenerator.Emit(OpCodes.Nop);
            ctrIlGenerator.Emit(OpCodes.Ret);

            // Generate methods implementation.
            foreach (var method in interfaceType.GetMethods())
            {
                // Generate signature.
                var methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot);
                var methodParameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
                methodBuilder.SetParameters(methodParameterTypes);
                methodBuilder.SetReturnType(method.ReturnType);

                // Generate method logic.
                var ilGenerator = methodBuilder.GetILGenerator();

                ilGenerator.Emit(OpCodes.Nop, (byte)0);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldstr, interfaceType.Name);
                ilGenerator.Emit(OpCodes.Ldstr, method.Name);
                ilGenerator.Emit(OpCodes.Ldc_I4, methodParameterTypes.Length);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                for (int i = 0; i < methodParameterTypes.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Dup);
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                    if (methodParameterTypes[i].IsValueType)
                        ilGenerator.Emit(OpCodes.Box, methodParameterTypes[i]);

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }

                var executeMethod = typeof(ProxyService).GetMethod(ProxyService.ExecuteMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
                ilGenerator.Emit(OpCodes.Callvirt, executeMethod);

                if (method.ReturnType == typeof(void))
                {
                    ilGenerator.Emit(OpCodes.Pop);
                }
                else if (method.ReturnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, method.ReturnType);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Castclass, method.ReturnType);
                }

                ilGenerator.Emit(OpCodes.Ret);
            }

            // Generate type and return.
            var generatedType = typeBuilder.CreateTypeInfo();
            return generatedType;
        }

        private static ModuleBuilder GetOrCreateModule()
        {
            if (_moduleBuilder == null)
            {
                lock (_assemblyInitLock)
                {
                    if (_moduleBuilder == null)
                    {
                        var generatedAssemblyName = $"{nameof(GenericRpc)}.Generated";
                        AssemblyName assemblyName = new AssemblyName(generatedAssemblyName);
                        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                        _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
                    }
                }
            }

            return _moduleBuilder;
        }
    }
}
