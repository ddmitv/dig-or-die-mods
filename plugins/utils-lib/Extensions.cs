using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ModUtils.Extensions;

public static class CodeMatcherExtensions {
    public static CodeMatcher InjectAndAdvance(this CodeMatcher self, OpCode opcode, object operand = null) {
        var prevInstruction = self.Instruction.Clone();
        self.SetAndAdvance(opcode, operand);
        self.Insert(prevInstruction);
        return self;
    }
    public static CodeMatcher GetOperand<T>(this CodeMatcher self, out T result) {
        if (self.Operand is not T operand) {
            throw new InvalidCastException($"Cannot convert operand to type '{typeof(T).FullName}'");
        }
        result = operand;
        return self;
    }
    public static CodeMatcher GetOperandAtOffset<T>(this CodeMatcher self, int offset, out T result) {
        if (self.Instructions()[self.Pos + offset].operand is not T operand) {
            throw new InvalidCastException($"Cannot convert operand to type '{typeof(T).FullName}'");
        }
        result = operand;
        return self;
    }
    public static CodeMatcher GetLabels(this CodeMatcher self, out List<Label> labels) {
        labels = self.Labels;
        return self;
    }
    public static CodeMatcher GetInstruction(this CodeMatcher self, out CodeInstruction instruction) {
        instruction = self.Instruction;
        return self;
    }
    public static CodeMatcher CollapseInstructions(this CodeMatcher self, uint count) {
        List<Label> labels = new List<Label>();
        for (int i = self.Pos; i < self.Pos + count; i++) {
            labels.AddRange(self.Instructions()[i].labels);
        }
        self.RemoveInstructions((int)count);
        self.AddLabels(labels.Distinct());

        return self;
    }
    public static CodeMatcher CollapseInstructionsTo(this CodeMatcher self, uint count, out List<Label> outLabels) {
        List<Label> labels = new List<Label>();
        for (int i = self.Pos; i < self.Pos + count; i++) {
            labels.AddRange(self.Instructions()[i].labels);
        }
        self.RemoveInstructions((int)count);
        outLabels = labels;

        return self;
    }
    public static CodeMatcher SetOperand(this CodeMatcher self, object operand) {
        self.Operand = operand;
        return self;
    }

    public static CodeMatcher CreateLabelAtOffset(this CodeMatcher self, int offset, out Label label) {
        self.CreateLabelAt(self.Pos + offset, out label);
        return self;
    }
    public static CodeMatcher SetOpcode(this CodeMatcher self, OpCode opcode) {
        self.Opcode = opcode;
        return self;
    }

    public static CodeMatcher ReplaceInstructionAndAdvance(this CodeMatcher self, CodeInstruction instruction) {
        // keep previous labels
        self.SetAndAdvance(instruction.opcode, instruction.operand);
        return self;
    }
    public static CodeMatcher ReplaceInstructionsAndAdvance(this CodeMatcher self, params CodeInstruction[] instructions) {
        foreach (var instr in instructions) {
            // keep previous labels
            self.SetAndAdvance(instr.opcode, instr.operand);
        }
        return self;
    }
}

public static class CodeMatchExtensions {
    public static CodeMatch LocalIndex(this CodeMatch self, int index) {
        return new CodeMatch(instruction => {
            if (self.predicate is not null && !self.predicate(instruction)) { return false; }
            if (self.opcodes.Count > 0 && !self.opcodes.Contains(instruction.opcode)) { return false; }
            if (self.operands.Count > 0 && !self.operands.Contains(instruction.operand)) { return false; }
            if (self.labels.Count > 0 && !self.labels.Intersect(instruction.labels).Any()) { return false; }
            if (self.blocks.Count > 0 && !self.blocks.Intersect(instruction.blocks).Any()) { return false; }

            if (instruction.operand is not LocalBuilder localBuilder) { return false; }
            return localBuilder.LocalIndex == index;
        }, self.name) {
            opcodes = new(self.opcodes), operands = new(self.operands), labels = new(self.labels),
            blocks = new(self.blocks), jumpsFrom = new(self.jumpsFrom), jumpsTo = new(self.jumpsTo),
        };
    }
}

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
}

public static class RectIntExtensions {
    public static RectInt Intersection(this RectInt self, RectInt other) {
        int x = Math.Max(self.x, other.x);
        int y = Math.Max(self.y, other.y);
        int width = Math.Min(self.xMax, other.xMax) - x;
        int height = Math.Min(self.yMax, other.yMax) - y;
        return new RectInt(x, y, width, height);
    }
}

public static class Vector2Extensions {
    public static Vector2 RotateRight(this Vector2 self) {
        return new Vector2(self.y, -self.x);
    }
    public static Vector2 RotateLeft(this Vector2 self) {
        return new Vector2(-self.y, self.x);
    }
}
