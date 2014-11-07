using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace PoorMan.KeyValueStore.Interception
{
    public class ProxyFactory
    {
        private static readonly Hashtable OpCodeTypeMapper = new Hashtable();
        
        private const string AssemblyName = "ProxyAssembly";
        private const string ModuleName = "ProxyModule";
        private const string InterceptorSetter = "SetInterceptor";
        private const string InterceptorName = "_callInterceptor";
        
        static ProxyFactory()
        {
            OpCodeTypeMapper.Add(typeof(Boolean), OpCodes.Ldind_I1);
            OpCodeTypeMapper.Add(typeof(Int16), OpCodes.Ldind_I2);
            OpCodeTypeMapper.Add(typeof(Int32), OpCodes.Ldind_I4);
            OpCodeTypeMapper.Add(typeof(Int64), OpCodes.Ldind_I8);
            OpCodeTypeMapper.Add(typeof(Double), OpCodes.Ldind_R8);
            OpCodeTypeMapper.Add(typeof(Single), OpCodes.Ldind_R4);
            OpCodeTypeMapper.Add(typeof(UInt16), OpCodes.Ldind_U2);
            OpCodeTypeMapper.Add(typeof(UInt32), OpCodes.Ldind_U4);
        }
        
        public object Create(Type type)
        {
            string typeName = type.FullName + "Proxy";
            type = CreateType(type, typeName);
            return Activator.CreateInstance(type);
        }

        private Type CreateType(Type parentType, string dynamicTypeName)
        {
            AppDomain domain = Thread.GetDomain();
            var assemblyName = new AssemblyName();
            assemblyName.Name = AssemblyName;
            assemblyName.Version = new Version(1, 0, 0, 0);
                
            AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName);
                
            TypeBuilder typeBuilder = moduleBuilder.DefineType(dynamicTypeName, 
                TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, parentType, new []{typeof(IInterceptorSetter)});
            FieldBuilder handlerField = typeBuilder.DefineField(InterceptorName, typeof(ICallInterceptor), FieldAttributes.Private);
                
            typeBuilder.AddInterfaceImplementation(typeof(IInterceptorSetter));
            var setInterceptorMethod = typeBuilder.DefineMethod(InterceptorSetter, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new[] { typeof(ICallInterceptor) });

            ILGenerator setInterceptorIl = setInterceptorMethod.GetILGenerator();
            setInterceptorIl.Emit(OpCodes.Ldarg_0);
            setInterceptorIl.Emit(OpCodes.Ldarg_1);
            setInterceptorIl.Emit(OpCodes.Stfld, handlerField);
            setInterceptorIl.Emit(OpCodes.Ret);    
               
            GenerateMethod(parentType, handlerField, typeBuilder);
                
            return typeBuilder.CreateType();
        }

        private void GenerateMethod(Type interfaceType, FieldBuilder handlerField, TypeBuilder typeBuilder)
        {
            MetaDataFactory.Add(interfaceType);
            MethodInfo[] interfaceMethods = interfaceType.GetMethods();
            for (int i = 0; i < interfaceMethods.Length; i++)
            {
                MethodInfo methodInfo = interfaceMethods[i];
                ParameterInfo[] methodParams = methodInfo.GetParameters();
                int numOfParams = methodParams.Length;
                var methodParameters = new Type[numOfParams];

                for (int j = 0; j < numOfParams; j++)
                {
                    methodParameters[j] = methodParams[j].ParameterType;
                }

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, methodInfo.ReturnType, methodParameters);
               
                ILGenerator methodIL = methodBuilder.GetILGenerator();
                if (!(methodInfo.ReturnType == typeof(void)))
                {
                    methodIL.DeclareLocal(methodInfo.ReturnType);
                    if (methodInfo.ReturnType.IsValueType && !methodInfo.ReturnType.IsPrimitive)
                    {
                        methodIL.DeclareLocal(methodInfo.ReturnType);
                    }
                }
                
                if (numOfParams > 0)
                {
                    methodIL.DeclareLocal(typeof(Object[]));
                }

                Label handlerLabel = methodIL.DefineLabel();
                Label returnLabel = methodIL.DefineLabel();

                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldfld, handlerField);
                methodIL.Emit(OpCodes.Brtrue_S, handlerLabel);
                if (!(methodInfo.ReturnType == typeof(void)))
                {
                    if (methodInfo.ReturnType.IsValueType && !methodInfo.ReturnType.IsPrimitive && !methodInfo.ReturnType.IsEnum)
                    {
                        methodIL.Emit(OpCodes.Ldloc_1);
                    }
                    else
                    {
                        methodIL.Emit(OpCodes.Ldnull);
                    }
                    methodIL.Emit(OpCodes.Stloc_0);
                    methodIL.Emit(OpCodes.Br_S, returnLabel);
                }

                methodIL.MarkLabel(handlerLabel);

                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldfld, handlerField);
                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Ldstr, interfaceType.FullName);
                methodIL.Emit(OpCodes.Ldc_I4, i);
                methodIL.Emit(OpCodes.Call, typeof(MetaDataFactory).GetMethod("GetMethod", new[] { typeof(string), typeof(int) }));

                methodIL.Emit(OpCodes.Ldc_I4, numOfParams);
                methodIL.Emit(OpCodes.Newarr, typeof(Object));

                if (numOfParams > 0)
                {
                    methodIL.Emit(OpCodes.Stloc_1);
                    for (int j = 0; j < numOfParams; j++)
                    {
                        methodIL.Emit(OpCodes.Ldloc_1);
                        methodIL.Emit(OpCodes.Ldc_I4, j);
                        methodIL.Emit(OpCodes.Ldarg, j + 1);
                        if (methodParameters[j].IsValueType)
                        {
                            methodIL.Emit(OpCodes.Box, methodParameters[j]);
                        }
                        methodIL.Emit(OpCodes.Stelem_Ref);
                    }
                    methodIL.Emit(OpCodes.Ldloc_1);
                }

                methodIL.Emit(OpCodes.Callvirt, typeof(ICallInterceptor).GetMethod("Invoke"));

                if (!(methodInfo.ReturnType == typeof(void)))
                {
                    if (methodInfo.ReturnType.IsValueType)
                    {
                        methodIL.Emit(OpCodes.Unbox, methodInfo.ReturnType);
                        if (methodInfo.ReturnType.IsEnum)
                        {
                            methodIL.Emit(OpCodes.Ldind_I4);
                        }
                        else if (!methodInfo.ReturnType.IsPrimitive)
                        {
                            methodIL.Emit(OpCodes.Ldobj, methodInfo.ReturnType);
                        }
                        else
                        {
                            methodIL.Emit((OpCode)OpCodeTypeMapper[methodInfo.ReturnType]);
                        }
                    }

                    methodIL.Emit(OpCodes.Stloc_0);
                    methodIL.Emit(OpCodes.Br_S, returnLabel);
                    methodIL.MarkLabel(returnLabel);
                    methodIL.Emit(OpCodes.Ldloc_0);
                }
                else
                {
                    methodIL.Emit(OpCodes.Pop);
                    methodIL.MarkLabel(returnLabel);
                }

                methodIL.Emit(OpCodes.Ret);
            }

            foreach (Type parentType in interfaceType.GetInterfaces())
            {
                GenerateMethod(parentType, handlerField, typeBuilder);
            }
        }
    }
}
