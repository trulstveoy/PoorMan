using System;
using System.Collections.Generic;
using KeyValueStore.Tests.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoorMan.KeyValueStore;

namespace KeyValueStore.Tests
{
    [TestClass]
    public class LazyLoad
    {
        [TestMethod]
        public void LazyLoadChildren()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(Product)).Create();
            datacontext.EnsureNewDatabase();

            var id = Guid.NewGuid();
            var order = new Order() {Id = id, Text = "Abc"};
            var p1 = new Product() { Id = Guid.NewGuid(), Text = "P1" };
            var p2 = new Product() { Id = Guid.NewGuid(), Text = "P2" };

            datacontext.Create(order);
            datacontext.Create(p1);
            datacontext.Create(p2);
            datacontext.AppendChild(order, p1);
            datacontext.AppendChild(order, p2);

            var result = datacontext.ReadWithRelations<Order>(id);
            List<Product> products = result.Products;
            Assert.AreEqual(2, products.Count);
        }

        [TestMethod]
        public void LazyLoadParent()
        {
            var datacontext = new Configuration(Constants.Connectionstring).WithDocuments(typeof(Order), typeof(OrderLine)).Create();
            datacontext.EnsureNewDatabase();
            
            var order = new Order() { Id = Guid.NewGuid(), Text = "Abc" };
            var line = new OrderLine() { Id = Guid.NewGuid(), OrderId = order.Id };
            
            datacontext.Create(order);
            datacontext.Create(line);

            var result = datacontext.ReadWithRelations<OrderLine>(line.Id);
            Assert.IsNotNull(result.Order);
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
