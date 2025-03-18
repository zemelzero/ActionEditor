using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 反射工具类，提供一系列静态方法用于处理反射相关操作
    /// </summary>
    public static class ReflectionTools
    {
#if !NETFX_CORE
        private const BindingFlags flagsEverything = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
#endif

        /// <summary>
        /// 已加载的程序集列表
        /// </summary>
        private static List<Assembly> _loadedAssemblies;

        /// <summary>
        /// 获取已加载的程序集列表
        /// </summary>
        private static List<Assembly> LoadedAssemblies
        {
            get
            {
                if (_loadedAssemblies == null)
                {
#if NETFX_CORE
				    _loadedAssemblies = new List<Assembly>();
		 		    var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
				    var folderFilesAsync = folder.GetFilesAsync();
				    folderFilesAsync.AsTask().Wait();

				    foreach (var file in folderFilesAsync.GetResults()){
				        if (file.FileType == ".dll" || file.FileType == ".exe"){
				            try
				            {
				                var filename = file.Name.Substring(0, file.Name.Length - file.FileType.Length);
				                AssemblyName name = new AssemblyName { Name = filename };
				                Assembly asm = Assembly.Load(name);
				                _loadedAssemblies.Add(asm);
				            }
				            catch { continue; }
				        }
				    }

#else

                    _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

#endif
                }

                return _loadedAssemblies;
            }
        }

        /// <summary>
        /// 类型名称与类型的映射字典
        /// </summary>
        private static readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type>();

        /// <summary>
        /// 根据类型名称获取类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>对应的类型</returns>
        public static Type GetType(string typeName)
        {
            if (typeMap.TryGetValue(typeName, out var type))
            {
                return type;
            }

            type = Type.GetType(typeName);
            if (type != null)
            {
                return typeMap[typeName] = type;
            }

            foreach (var asm in LoadedAssemblies)
            {
                try
                {
                    type = asm.GetType(typeName);
                }
                catch
                {
                    continue;
                }

                if (type != null)
                {
                    return typeMap[typeName] = type;
                }
            }

            // 最坏的情况，遍历所有类型
            foreach (var t in GetAllTypes())
            {
                if (t.Name == typeName)
                {
                    return typeMap[typeName] = t;
                }
            }

            Debug.LogError($"Requested Type with name '{typeName}', could not be loaded");
            return null;
        }

        /// <summary>
        /// 获取所有已加载的类型
        /// </summary>
        /// <returns>所有已加载的类型数组</returns>
        public static Type[] GetAllTypes()
        {
            var result = new List<Type>();
            foreach (var asm in LoadedAssemblies)
            {
                try
                {
                    result.AddRange(asm.RTGetExportedTypes());
                }
                catch
                {
                    continue;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 类型与其子类型的映射字典
        /// </summary>
        private static readonly Dictionary<Type, Type[]> subTypesMap = new Dictionary<Type, Type[]>();

        /// <summary>
        /// 获取指定类型的所有子类型
        /// </summary>
        /// <param name="type">基类型</param>
        /// <returns>所有子类型数组</returns>
        public static Type[] GetImplementationsOf(Type type)
        {
            if (subTypesMap.TryGetValue(type, out var result))
            {
                return result;
            }

            var temp = new List<Type>();
            foreach (var asm in LoadedAssemblies)
            {
                try
                {
                    temp.AddRange(asm.RTGetExportedTypes().Where(t => type.RTIsAssignableFrom(t) && !t.RTIsAbstract()));
                }
                catch
                {
                    continue;
                }
            }

            return subTypesMap[type] = temp.ToArray();
        }

        /// <summary>
        /// 获取程序集的所有导出类型
        /// </summary>
        /// <param name="asm">程序集</param>
        /// <returns>所有导出类型数组</returns>
        private static Type[] RTGetExportedTypes(this Assembly asm)
        {
#if NETFX_CORE
			return asm.ExportedTypes.ToArray();
#else
            return asm.GetExportedTypes();
#endif
        }

        /// <summary>
        /// 获取类型的友好名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>友好名称</returns>
        public static string FriendlyName(this Type type)
        {
            if (type == null)
            {
                return "NULL";
            }

            if (type == typeof(float))
            {
                return "Float";
            }

            if (type == typeof(int))
            {
                return "Integer";
            }

            return type.Name;
        }

        /// <summary>
        /// 判断属性是否为静态
        /// </summary>
        /// <param name="propertyInfo">属性信息</param>
        /// <returns>是否为静态</returns>
        public static bool RTIsStatic(this PropertyInfo propertyInfo)
        {
            return ((propertyInfo.CanRead && propertyInfo.RTGetGetMethod().IsStatic) ||
                    (propertyInfo.CanWrite && propertyInfo.RTGetSetMethod().IsStatic));
        }

        /// <summary>
        /// 判断类型是否为抽象类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否为抽象类型</returns>
        public static bool RTIsAbstract(this Type type)
        {
#if NETFX_CORE
			return type.GetTypeInfo().IsAbstract;
#else
            return type.IsAbstract;
#endif
        }

        /// <summary>
        /// 判断类型是否为指定类型的子类
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="other">基类型</param>
        /// <returns>是否为子类</returns>
        public static bool RTIsSubclassOf(this Type type, Type other)
        {
#if NETFX_CORE
			return type.GetTypeInfo().IsSubclassOf(other);
#else
            return type.IsSubclassOf(other);
#endif
        }

        /// <summary>
        /// 判断类型是否可以从指定类型赋值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="second">基类型</param>
        /// <returns>是否可以从指定类型赋值</returns>
        public static bool RTIsAssignableFrom(this Type type, Type second)
        {
#if NETFX_CORE
			return type.GetTypeInfo().IsAssignableFrom(second.GetTypeInfo());
#else
            return type.IsAssignableFrom(second);
#endif
        }

        /// <summary>
        /// 获取类型的字段信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">字段名称</param>
        /// <returns>字段信息</returns>
        public static FieldInfo RTGetField(this Type type, string name)
        {
#if NETFX_CORE
			return type.GetRuntimeFields().FirstOrDefault(f => f.Name == name);
#else
            return type.GetField(name, flagsEverything);
#endif
        }

        /// <summary>
        /// 获取类型的属性信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">属性名称</param>
        /// <returns>属性信息</returns>
        public static PropertyInfo RTGetProperty(this Type type, string name)
        {
#if NETFX_CORE
			return type.GetRuntimeProperties().FirstOrDefault(p => p.Name == name);
#else
            return type.GetProperty(name, flagsEverything);
#endif
        }

        /// <summary>
        /// 获取类型的方法信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">方法名称</param>
        /// <returns>方法信息</returns>
        public static MethodInfo RTGetMethod(this Type type, string name)
        {
#if NETFX_CORE
			return type.GetRuntimeMethods().FirstOrDefault(m => m.Name == name);
#else
            return type.GetMethod(name, flagsEverything);
#endif
        }

        /// <summary>
        /// 获取类型的所有字段信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>所有字段信息数组</returns>
        public static FieldInfo[] RTGetFields(this Type type)
        {
#if NETFX_CORE
			return type.GetRuntimeFields().ToArray();
#else
            return type.GetFields(flagsEverything);
#endif
        }

        /// <summary>
        /// 获取类型的所有属性信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>所有属性信息数组</returns>
        public static PropertyInfo[] RTGetProperties(this Type type)
        {
#if NETFX_CORE
			return type.GetRuntimeProperties().ToArray();
#else
            return type.GetProperties(flagsEverything);
#endif
        }

        /// <summary>
        /// 获取属性的get方法信息
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>get方法信息</returns>
        public static MethodInfo RTGetGetMethod(this PropertyInfo prop)
        {
#if NETFX_CORE
			return prop.GetMethod;
#else
            return prop.GetGetMethod();
#endif
        }

        /// <summary>
        /// 获取属性的set方法信息
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>set方法信息</returns>
        public static MethodInfo RTGetSetMethod(this PropertyInfo prop)
        {
#if NETFX_CORE
			return prop.SetMethod;
#else
            return prop.GetSetMethod();
#endif
        }

        /// <summary>
        /// 获取类型的反射类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>反射类型</returns>
        public static Type RTReflectedType(this Type type)
        {
#if NETFX_CORE
			return type.GetTypeInfo().DeclaringType; // 无法获取ReflectedType
#else
            return type.ReflectedType;
#endif
        }

        /// <summary>
        /// 获取成员的反射类型
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>反射类型</returns>
        public static Type RTReflectedType(this MemberInfo member)
        {
#if NETFX_CORE
			return member.DeclaringType; // 无法获取ReflectedType
#else
            return member.ReflectedType;
#endif
        }

        /// <summary>
        /// 获取类型的指定特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherited">是否继承</param>
        /// <returns>特性实例</returns>
        public static T RTGetAttribute<T>(this Type type, bool inherited) where T : Attribute
        {
#if NETFX_CORE
			return (T)type.GetTypeInfo().GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#else
            return (T)type.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#endif
        }

        /// <summary>
        /// 获取成员的指定特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="member">成员信息</param>
        /// <param name="inherited">是否继承</param>
        /// <returns>特性实例</returns>
        public static T RTGetAttribute<T>(this MemberInfo member, bool inherited) where T : Attribute
        {
#if NETFX_CORE
			return (T)member.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#else
            return (T)member.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#endif
        }

        /// <summary>
        /// 判断成员是否定义了指定特性
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="member">成员信息</param>
        /// <param name="inherited">是否继承</param>
        /// <returns>是否定义了指定特性</returns>
        public static bool RTIsDefined<T>(this MemberInfo member, bool inherited) where T : Attribute
        {
#if NETFX_CORE
			return member.IsDefined(typeof(T), inherited);
#else
            return member.IsDefined(typeof(T), inherited);
#endif
        }

        /// <summary>
        /// 为方法创建委托
        /// </summary>
        /// <typeparam name="T">委托类型</typeparam>
        /// <param name="method">方法信息</param>
        /// <param name="instance">实例</param>
        /// <returns>委托实例</returns>
        public static T RTCreateDelegate<T>(this MethodInfo method, object instance)
        {
#if NETFX_CORE
			return (T)(object)method.CreateDelegate(typeof(T), instance);
#else
            return (T)(object)Delegate.CreateDelegate(typeof(T), instance, method);
#endif
        }

        /// <summary>
        /// 创建并返回字段或属性的setter
        /// </summary>
        /// <typeparam name="T">实例类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="info">成员信息</param>
        /// <returns>setter委托</returns>
        public static Action<T, TValue> GetFieldOrPropSetter<T, TValue>(MemberInfo info)
        {
            return (x, v) => RTSetFieldOrPropValue(info, x, v);
        }

        /// <summary>
        /// 获取类型的所有字段和属性信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>所有字段和属性信息数组</returns>
        public static MemberInfo[] RTGetFieldsAndProps(this Type type)
        {
            var result = new List<MemberInfo>();
            result.AddRange(type.RTGetFields());
            result.AddRange(type.RTGetProperties());
            return result.ToArray();
        }

        /// <summary>
        /// 获取类型的字段或属性信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">成员名称</param>
        /// <returns>字段或属性信息</returns>
        public static MemberInfo RTGetFieldOrProp(this Type type, string name)
        {
            MemberInfo result = type.RTGetField(name);
            if (result == null)
            {
                result = type.RTGetProperty(name);
            }

            return result;
        }

        /// <summary>
        /// 获取字段或属性的值
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="instance">实例</param>
        /// <param name="index">索引</param>
        /// <returns>字段或属性的值</returns>
        public static object RTGetFieldOrPropValue(this MemberInfo member, object instance, int index = -1)
        {
            if (member is FieldInfo info)
            {
                return info.GetValue(instance);
            }

            if (member is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(instance, index == -1 ? null : new object[] { index });
            }

            return null;
        }

        /// <summary>
        /// 设置字段或属性的值
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="instance">实例</param>
        /// <param name="value">值</param>
        /// <param name="index">索引</param>
        public static void RTSetFieldOrPropValue(this MemberInfo member, object instance, object value, int index = -1)
        {
            if (member is FieldInfo info)
            {
                info.SetValue(instance, value);
            }

            if (member is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(instance, value, index == -1 ? null : new object[] { index });
            }
        }

        /// <summary>
        /// 获取字段或属性的类型
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>字段或属性的类型</returns>
        public static Type RTGetFieldOrPropType(this MemberInfo member)
        {
            if (member is FieldInfo info)
            {
                return info.FieldType;
            }

            if (member is PropertyInfo propertyInfo)
            {
                return propertyInfo.PropertyType;
            }

            return null;
        }

        /// <summary>
        /// 根据实例和路径获取成员信息
        /// </summary>
        /// <param name="root">根实例</param>
        /// <param name="path">成员路径</param>
        /// <returns>成员信息</returns>
        public static MemberInfo GetRelativeMember(object root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            return GetRelativeMember(root.GetType(), path);
        }

        /// <summary>
        /// 根据类型和路径获取成员信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="path">成员路径</param>
        /// <returns>成员信息</returns>
        public static MemberInfo GetRelativeMember(Type type, string path)
        {
            if (type == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            MemberInfo result = null;
            var parts = path.Split('.');
            if (parts.Length == 1)
            {
                return type.RTGetFieldOrProp(parts[0]);
            }

            foreach (var part in parts)
            {
                result = type.RTGetFieldOrProp(part);
                if (result == null)
                {
                    return null;
                }

                type = result.RTGetFieldOrPropType();
                if (type == null)
                {
                    return null;
                }
            }

            return result;
        }

        /// <summary>
        /// 根据根对象和相对成员路径，返回包含叶成员的对象
        /// </summary>
        /// <param name="root">根对象</param>
        /// <param name="path">相对成员路径</param>
        /// <returns>包含叶成员的对象</returns>
        public static object GetRelativeMemberParent(object root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var parts = path.Split('.');
            if (parts.Length == 1)
            {
                return root;
            }

            var member = root.GetType().RTGetFieldOrProp(parts[0]);
            if (member == null)
            {
                return null;
            }

            root = member.RTGetFieldOrPropValue(root);
            return GetRelativeMemberParent(root, string.Join(".", parts, 1, parts.Length - 1));
        }

        /// <summary>
        /// 根据表达式返回相对路径，例如：'(Transform x) => x.position' 返回 'position'
        /// </summary>
        /// <typeparam name="T">根对象类型</typeparam>
        /// <typeparam name="TResult">成员类型</typeparam>
        /// <param name="func">表达式</param>
        /// <returns>相对路径</returns>
        public static string GetMemberPath<T, TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> func)
        {
            var result = func.Body.ToString();
            return result.Substring(result.IndexOf('.') + 1);
        }

        /// <summary>
        /// 递归地从提供的起始类型中挖掘并返回符合谓词条件的实例或公共属性/字段路径
        /// </summary>
        /// <param name="type">起始类型</param>
        /// <param name="shouldInclude">是否包含该类型的谓词</param>
        /// <param name="shouldContinue">是否继续递归的谓词</param>
        /// <param name="currentPath">当前路径</param>
        /// <param name="recursionCheck">递归检查列表</param>
        /// <returns>成员路径数组</returns>
        public static string[] GetMemberPaths(Type type, Predicate<Type> shouldInclude, Predicate<Type> shouldContinue,
            string currentPath = "", List<Type> recursionCheck = null)
        {
            var result = new List<string>();
            if (recursionCheck == null)
            {
                recursionCheck = new List<Type>();
            }

            if (recursionCheck.Contains(type))
            {
                return result.ToArray();
            }

            recursionCheck.Add(type);
            foreach (var _prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var prop = _prop;
                if (prop.CanRead && prop.CanWrite && shouldInclude(prop.PropertyType))
                {
                    result.Add(currentPath + prop.Name);
                    continue;
                }

                if (prop.CanRead && shouldContinue(prop.PropertyType))
                {
                    result.AddRange(GetMemberPaths(prop.PropertyType, shouldInclude, shouldContinue,
                        currentPath + prop.Name + ".", recursionCheck));
                }
            }

            foreach (var _field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var field = _field;
                if (shouldInclude(field.FieldType))
                {
                    result.Add(currentPath + field.Name);
                    continue;
                }

                if (shouldContinue(field.FieldType))
                {
                    result.AddRange(GetMemberPaths(field.FieldType, shouldInclude, shouldContinue,
                        currentPath + field.Name + ".", recursionCheck));
                }
            }

            return result.ToArray();
        }
    }
}