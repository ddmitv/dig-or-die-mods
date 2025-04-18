using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System;

namespace ModUtils.Extensions;

public static class CodeMatcherExtensions {
    public static CodeMatcher InjectAndAdvance(this CodeMatcher self, OpCode opcode, object operand = null) {
        var prevInstruction = self.Instruction.Clone();
        self.SetAndAdvance(opcode, operand);
        self.Insert(prevInstruction);
        return self;
    }
    public static CodeMatcher GetOperand<T>(this CodeMatcher self, out T result) {
        result = (T)self.Operand;
        return self;
    }
    public static CodeMatcher GetOperandAtOffset<T>(this CodeMatcher self, int offset, out T result) {
        result = (T)self.Instructions()[self.Pos + offset].operand;
        return self;
    }
    public static CodeMatcher GetLabels(this CodeMatcher self, out List<Label> labels) {
        labels = self.Labels;
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

    public static FieldInfo Field(this Type type, string name) {
        FieldInfo fieldInfo = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fieldInfo ?? throw new MissingFieldException($"Field '{name}' not found in {type.FullName}.");
    }
    public static FieldInfo StaticField(this Type type, string name) {
        FieldInfo fieldInfo = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return fieldInfo ?? throw new MissingFieldException($"Static field '{name}' not found in {type.FullName}.");
    }
}
