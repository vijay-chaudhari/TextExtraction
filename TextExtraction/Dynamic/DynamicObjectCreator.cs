using System.Reflection;
using System.Reflection.Emit;

namespace TextExtraction.Dynamic
{
    public static class DynamicObjectCreator
    {
        public static object CreateDynamicObject(string typeName)
        {
            // Create a new assembly
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            // Create a new module in the assembly
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            // Create a new type builder for the dynamic type
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            // Define properties for the dynamic type
            typeBuilder.DefineProperty("Text", PropertyAttributes.None, typeof(string), null);
            typeBuilder.DefineProperty("PageNumber", PropertyAttributes.None, typeof(int), null);
            typeBuilder.DefineProperty("Rectangle", PropertyAttributes.None, typeof(string), null);

            // Create the dynamic type
            var dynamicType = typeBuilder.CreateType();

            // Create an instance of the dynamic type
            var dynamicObject = Activator.CreateInstance(dynamicType);

            return dynamicObject;
        }
    }
}
