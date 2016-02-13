using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Zeus
{
    public class Util
    {
        private static void GetAllDerivedTypesRecursively(Type[] types, Type type1, ref List<Type> results)
        {
            if (type1.IsGenericType)
            {
                GetDerivedFromGeneric(types, type1, ref results);
            }
            else
            {
                GetDerivedFromNonGeneric(types, type1, ref results);
            }
        }

        private static void GetDerivedFromGeneric(Type[] types, Type type, ref List<Type> results)
        {
            var derivedTypes = types
                .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
                            t.BaseType.GetGenericTypeDefinition() == type).ToList();
            results.AddRange(derivedTypes);
            foreach (Type derivedType in derivedTypes)
            {
                GetAllDerivedTypesRecursively(types, derivedType, ref results);
            }
        }


        public static void GetDerivedFromNonGeneric(Type[] types, Type type, ref List<Type> results)
        {
            var derivedTypes = types.Where(t => t != type && type.IsAssignableFrom(t)).ToList();

            results.AddRange(derivedTypes);
            foreach (Type derivedType in derivedTypes)
            {
                GetAllDerivedTypesRecursively(types, derivedType, ref results);
            }
        }        
        
        public static Delegate CreateDelegate(ConstructorInfo constructor, Type delegateType)
        {
            if (constructor == null)
            {
                throw new ArgumentNullException("constructor");
            }
            if (delegateType == null)
            {
                throw new ArgumentNullException("delegateType");
            }

            // Validate the delegate return type
            MethodInfo delMethod = delegateType.GetMethod("Invoke");

            if (constructor.DeclaringType.IsAssignableFrom(delMethod.ReturnType))
            {
                //throw new InvalidOperationException("The return type of the delegate must match the constructors delclaring type");
                System.Diagnostics.Debug.WriteLine("The return type of the delegate must match the constructors delclaring type");
            }


            // Validate the signatures
            ParameterInfo[] delParams = delMethod.GetParameters();
            ParameterInfo[] constructorParam = constructor.GetParameters();
            if (delParams.Length != constructorParam.Length)
            {
                throw new InvalidOperationException("The delegate signature does not match that of the constructor");
            }
            for (int i = 0; i < delParams.Length; i++)
            {
                if (delParams[i].ParameterType != constructorParam[i].ParameterType ||  // Probably other things we should check ??
                    delParams[i].IsOut)
                {
                    throw new InvalidOperationException("The delegate signature does not match that of the constructor");
                }
            }
            // Create the dynamic method
            DynamicMethod method =
                new DynamicMethod(
                    string.Format("{0}__{1}", constructor.DeclaringType.Name, Guid.NewGuid().ToString().Replace("-", "")),
                    constructor.DeclaringType,
                    Array.ConvertAll<ParameterInfo, Type>(constructorParam, p => p.ParameterType),
                    true
                    );

            // Create the il
            ILGenerator gen = method.GetILGenerator();
            for (int i = 0; i < constructorParam.Length; i++)
            {
                if (i < 4)
                {
                    switch (i)
                    {
                        case 0:
                            gen.Emit(OpCodes.Ldarg_0);
                            break;
                        case 1:
                            gen.Emit(OpCodes.Ldarg_1);
                            break;
                        case 2:
                            gen.Emit(OpCodes.Ldarg_2);
                            break;
                        case 3:
                            gen.Emit(OpCodes.Ldarg_3);
                            break;
                    }
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg_S, i);
                }
            }
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Ret);

            // Return the delegate :)
            return method.CreateDelegate(delegateType);

        }


        /// <summary>
        /// Unzips a file to a directory.
        /// </summary>
        /// <param name="zipFile">the file to unzip</param>
        /// <returns></returns>
        public static bool UnzipFile(string zipFile, string targetDirectory)
        {
            try
            {
                using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFile)))
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string path = Path.Combine(targetDirectory, theEntry.Name);                        
                        string directoryName = Path.GetDirectoryName(path);
                        string fileName = Path.GetFileName(path);

                        // create directory
                        if (directoryName.Length > 0 && !Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        if (fileName != String.Empty)
                        {
                            using (FileStream streamWriter = File.Create(Path.GetFullPath(path)))
                            {

                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed to unzip file [" + zipFile + "] to [" + targetDirectory + "].", e);
                return false;
            }

            return true;
        }
    }
}
