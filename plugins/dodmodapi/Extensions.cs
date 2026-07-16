
using System;
using System.Linq;
using System.Reflection;

namespace DODModAPI.Extensions;

public static class TypeExtensions {
    public static MethodInfo Method(this Type type, string name) {
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}' not found in {type.FullName}.");
    }
    public static MethodInfo Method(this Type type, string name, Type[] types) {
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}({string.Join(", ", types.Select(t => t.FullName).ToArray())})' not found in {type.FullName}.");
    }
    public static MethodInfo Method<T1>(this Type type, string name) {
        Type[] types = [typeof(T1)];
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}({string.Join(", ", types.Select(t => t.FullName).ToArray())})' not found in {type.FullName}.");
    }
    public static MethodInfo Method<T1, T2>(this Type type, string name) {
        Type[] types = [typeof(T1), typeof(T2)];
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}({string.Join(", ", types.Select(t => t.FullName).ToArray())})' not found in {type.FullName}.");
    }
    public static MethodInfo Method<T1, T2, T3>(this Type type, string name) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3)];
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}({string.Join(", ", types.Select(t => t.FullName).ToArray())})' not found in {type.FullName}.");
    }
    public static MethodInfo Method<T1, T2, T3, T4>(this Type type, string name) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
        MethodInfo methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return methodInfo ?? throw new MissingMethodException($"Method '{name}({string.Join(", ", types.Select(t => t.FullName).ToArray())})' not found in {type.FullName}.");
    }

    public static ConstructorInfo Constructor(this Type type, Type[] types) {
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return constructorInfo ?? throw new MissingMethodException($"Constructor {type.FullName}::.ctor({string.Join(", ", types.Select(t => t.FullName).ToArray())}) not found.");
    }
    public static ConstructorInfo Constructor<T1>(this Type type) {
        Type[] types = [typeof(T1)];
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return constructorInfo ?? throw new MissingMethodException($"Constructor {type.FullName}::.ctor({string.Join(", ", types.Select(t => t.FullName).ToArray())}) not found.");
    }
    public static ConstructorInfo Constructor<T1, T2>(this Type type) {
        Type[] types = [typeof(T1), typeof(T2)];
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return constructorInfo ?? throw new MissingMethodException($"Constructor {type.FullName}::.ctor({string.Join(", ", types.Select(t => t.FullName).ToArray())}) not found.");
    }
    public static ConstructorInfo Constructor<T1, T2, T3>(this Type type) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3)];
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return constructorInfo ?? throw new MissingMethodException($"Constructor {type.FullName}::.ctor({string.Join(", ", types.Select(t => t.FullName).ToArray())}) not found.");
    }
    public static ConstructorInfo Constructor<T1, T2, T3, T4>(this Type type) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        return constructorInfo ?? throw new MissingMethodException($"Constructor {type.FullName}::.ctor({string.Join(", ", types.Select(t => t.FullName).ToArray())}) not found.");
    }

    public static FieldInfo Field(this Type type, string name) {
        FieldInfo fieldInfo = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fieldInfo ?? throw new MissingFieldException($"Field '{name}' not found in {type.FullName}.");
    }
    public static FieldInfo StaticField(this Type type, string name) {
        FieldInfo fieldInfo = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return fieldInfo ?? throw new MissingFieldException($"Static field '{name}' not found in {type.FullName}.");
    }

    public static FieldInfo CoroutineField(this Type parentType, string coroutineClassName, string fieldName) {
        Type stateMachineType = parentType.GetNestedType(coroutineClassName, BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new TypeLoadException($"Could not find nested coroutine class \"{coroutineClassName}\" inside \"{parentType.FullName}\".");
        
        FieldInfo field = stateMachineType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException($"Could not find field \"{fieldName}\" inside coroutine \"{coroutineClassName}\".");

        return field;
    }
}
