using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ModUtils;

public static class CodeMatcherExtensions {
    public static CodeMatcher Inject(this CodeMatcher self, OpCode opcode, object operand = null) {
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
}
