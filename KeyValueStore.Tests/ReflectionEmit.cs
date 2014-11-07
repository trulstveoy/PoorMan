using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;
using PoorMan.KeyValueStore.Interception;

namespace KeyValueStore.Tests
{
    public class SampleInvocationHandler : ICallInterceptor
    {
        public object Invoke(object proxy, MethodInfo method, object[] parameters)
        {
            var retVal = method.Invoke(proxy, parameters);
            return retVal;
        }
    }

    [TestClass]
    public class ReflectionEmit
    {
        [TestMethod]
        public void LazyLoadChildren()
        {
            var datacontext = new Configuration(Constants.Connectionstring).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var p1Id = Guid.NewGuid();
            var p2Id = Guid.NewGuid();

            datacontext.Create(id, new Order() {Text = "Abc"});
            datacontext.Create(p1Id, new Product() {Text = "P1"});
            datacontext.Create(p2Id, new Product() {Text = "P2"});

            datacontext.AppendChild<Order, Product>(id, p1Id);
            datacontext.AppendChild<Order, Product>(id, p2Id);

            var order = datacontext.ReadWithChildren<Order>(id);
            List<Product> products = order.Products;
            foreach (var product in products)
            {
                
            }
        }

        [TestMethod]
        [Ignore]
        public void DoIt()
        {
            var fooProxy = new ProxyFactory().Create<Foo>();
            var proxyType = fooProxy.GetType();
            
            var foo = new Foo() { Num = 4554 };
            var s1 = new XmlSerializer(typeof(Foo));

            Foo newFoo;
            using (var m1 = new MemoryStream())
            {
                s1.Serialize(m1, foo);
                m1.Seek(0, SeekOrigin.Begin);

                var overrides = new XmlAttributeOverrides();
                overrides.Add(typeof(Foo), new XmlAttributes { XmlType = new XmlTypeAttribute("IgnoreFoo") });
                overrides.Add(proxyType, new XmlAttributes { XmlType = new XmlTypeAttribute("Foo") });

                var s2 = new XmlSerializer(proxyType, overrides);
                newFoo = (Foo)s2.Deserialize(m1);
            }

            var ifc = (IInterceptorSetter)newFoo;
            ifc.SetInterceptor(new SampleInvocationHandler());

            var bar = newFoo.Bar;
        }
    }
}

/*
 * 
 *         //    var assembly = new AssemblyName("FooExtension");
        //    AppDomain appDomain = System.Threading.Thread.GetDomain();

        //    AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
        //    ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assembly.Name);

        //    TypeBuilder typeBuilder = moduleBuilder.DefineType("FooProxy", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
        //                                            TypeAttributes.BeforeFieldInit, typeof(Foo));

        //    FieldBuilder firstNameField = typeBuilder.DefineField("theString", typeof(System.String), FieldAttributes.Private);
            

        //    PropertyBuilder propertyBuilder = typeBuilder.DefineProperty("TheString", PropertyAttributes.HasDefault, typeof(string), null);



        //    MethodBuilder propertyGetter = typeBuilder.DefineMethod("get_TheString", MethodAttributes.Public | MethodAttributes.SpecialName |
        //                                                          MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);

        //    ILGenerator propertyGetterIL = propertyGetter.GetILGenerator();
        //    propertyGetterIL.Emit(OpCodes.Ldarg_0);
        //    propertyGetterIL.Emit(OpCodes.Ldfld, firstNameField);
        //    propertyGetterIL.Emit(OpCodes.Ret);

        //    propertyBuilder.SetGetMethod(propertyGetter);


        //    var type = typeBuilder.CreateType();
        //    var propertyInfos = type.GetProperties();
        //    var theProp = propertyInfos.First();
            
        //    object instance = Activator.CreateInstance(type);

        //    var type1 = instance.GetType();

        //    var value = theProp.GetValue(instance);


        //    var foo = new Foo() {Num = 4554};
        //    var s1 = new XmlSerializer(typeof (Foo));
        //    Foo fooProxy;
        //    using (var m1 = new MemoryStream())
        //    {
        //        s1.Serialize(m1, foo);
        //        m1.Seek(0, SeekOrigin.Begin);

        //        var overrides = new XmlAttributeOverrides();
        //        overrides.Add(typeof(Foo), new XmlAttributes { XmlType = new XmlTypeAttribute("IgnoreFoo") });
        //        overrides.Add(type, new XmlAttributes{XmlType = new XmlTypeAttribute("Foo")});

        //        var s2 = new XmlSerializer(type, overrides);
        //        fooProxy = (Foo)s2.Deserialize(m1);
        //    }
        //}
 */

/*
 * 
AssemblyName assembly = new AssemblyName("FileHelpersTests");
AppDomain appDomain = System.Threading.Thread.GetDomain();
AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assembly.Name);

//create the class
TypeBuilder typeBuilder = moduleBuilder.DefineType("Person", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
                                                    TypeAttributes.BeforeFieldInit, typeof(System.Object));

//create the Delimiter attribute

//create the firstName field
FieldBuilder firstNameField = typeBuilder.DefineField("firstName", typeof(System.String), FieldAttributes.Private);

//create the firstName attribute [FieldOrder(0)]

//create the FirstName property
PropertyBuilder firstNameProperty = typeBuilder.DefineProperty("FirstName", PropertyAttributes.HasDefault, typeof(System.String), null);

//create the FirstName Getter
MethodBuilder firstNamePropertyGetter = typeBuilder.DefineMethod("get_FirstName", MethodAttributes.Public | MethodAttributes.SpecialName |
                                                                  MethodAttributes.HideBySig, typeof(System.String), Type.EmptyTypes);
ILGenerator firstNamePropertyGetterIL = firstNamePropertyGetter.GetILGenerator();
firstNamePropertyGetterIL.Emit(OpCodes.Ldarg_0);
firstNamePropertyGetterIL.Emit(OpCodes.Ldfld, firstNameField);
firstNamePropertyGetterIL.Emit(OpCodes.Ret);

//create the FirstName Setter
MethodBuilder firstNamePropertySetter = typeBuilder.DefineMethod("set_FirstName", MethodAttributes.Public | MethodAttributes.SpecialName |
                                                    MethodAttributes.HideBySig, null, new Type[] { typeof(System.String) });
ILGenerator firstNamePropertySetterIL = firstNamePropertySetter.GetILGenerator();
firstNamePropertySetterIL.Emit(OpCodes.Ldarg_0);
firstNamePropertySetterIL.Emit(OpCodes.Ldarg_1);
firstNamePropertySetterIL.Emit(OpCodes.Stfld, firstNameField);
firstNamePropertySetterIL.Emit(OpCodes.Ret);

//assign getter and setter
firstNameProperty.SetGetMethod(firstNamePropertyGetter);
firstNameProperty.SetSetMethod(firstNamePropertySetter);


//create the lastName field
FieldBuilder lastNameField = typeBuilder.DefineField("lastName", typeof(System.String), FieldAttributes.Private);

//create the lastName attribute [FieldOrder(1)]

//create the LastName property
PropertyBuilder lastNameProperty = typeBuilder.DefineProperty("LastName", PropertyAttributes.HasDefault, typeof(System.String), null);

//create the LastName Getter
MethodBuilder lastNamePropertyGetter = typeBuilder.DefineMethod("get_LastName", MethodAttributes.Public | MethodAttributes.SpecialName |
                                                                  MethodAttributes.HideBySig, typeof(System.String), Type.EmptyTypes);
ILGenerator lastNamePropertyGetterIL = lastNamePropertyGetter.GetILGenerator();
lastNamePropertyGetterIL.Emit(OpCodes.Ldarg_0);
lastNamePropertyGetterIL.Emit(OpCodes.Ldfld, lastNameField);
lastNamePropertyGetterIL.Emit(OpCodes.Ret);

//create the FirstName Setter
MethodBuilder lastNamePropertySetter = typeBuilder.DefineMethod("set_FirstName", MethodAttributes.Public | MethodAttributes.SpecialName |
                                                    MethodAttributes.HideBySig, null, new Type[] { typeof(System.String) });
ILGenerator lastNamePropertySetterIL = lastNamePropertySetter.GetILGenerator();
lastNamePropertySetterIL.Emit(OpCodes.Ldarg_0);
lastNamePropertySetterIL.Emit(OpCodes.Ldarg_1);
lastNamePropertySetterIL.Emit(OpCodes.Stfld, lastNameField);
lastNamePropertySetterIL.Emit(OpCodes.Ret);

//assign getter and setter
lastNameProperty.SetGetMethod(lastNamePropertyGetter);
lastNameProperty.SetSetMethod(lastNamePropertySetter);
 * 
 * */
