﻿using Celbridge.Models;
using Celbridge.Utils;
using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using Celbridge.Models.CelMixins;

namespace Celbridge.Tasks
{
    public class BuildApplicationTask
    {
        const string ApplicationName = "Application";

        public async Task<Result<string>> BuildApplication(List<ICelScript> celScripts, string libraryPath)
        {
            try
            {
                // Ensure the Application folder exists
                var applicationFolder = Path.Combine(libraryPath, ApplicationName);
                if (Directory.Exists(applicationFolder))
                {
                    Directory.Delete(applicationFolder, true);
                }
                Directory.CreateDirectory(applicationFolder);

                var environmentResult = await GenerateEnvironmentFile(applicationFolder);
                if (environmentResult is ErrorResult environmentError)
                {
                    return new ErrorResult<string>($"Failed to build application. {environmentError.Message}");
                }

                var generateResult = await GenerateSourceFiles(celScripts, applicationFolder);
                if (generateResult is ErrorResult generateError)
                {
                    return new ErrorResult<string>($"Failed to build application. {generateError.Message}");
                }

                var compileResult = await AssemblyUtils.CompileAssembly(applicationFolder, "CelEnvironment", new List<String>());
                if (compileResult is ErrorResult<string> compileError)
                {
                    return new ErrorResult<string>($"Failed to build application. {compileError.Message}");
                }

                return compileResult;
            }
            catch (Exception ex)
            {
                return new ErrorResult<string>($"Failed to build application. {ex.Message}");
            }
        }

        private async Task<Result> GenerateSourceFiles(List<ICelScript> celScripts, string applicationFolder)
        {
            foreach (var celScript in celScripts)
            {
                var celScriptEntity = celScript.Entity;
                Guard.IsNotNull(celScriptEntity);

                var celScriptName = celScriptEntity.Name;
                celScriptName = Path.GetFileNameWithoutExtension(celScriptName);

                var cels = celScript.Cels.ToList();

                var generateResult = await GenerateSourceFile(celScriptName, cels, applicationFolder);
                if (generateResult is ErrorResult generateError)
                {
                    return new ErrorResult($"Failed to generate source files. {generateError.Message}");
                }
            }

            return new SuccessResult();
        }

        private async Task<Result> GenerateSourceFile(string celScriptName, List<ICelScriptNode> cels, string applicationFolder)
        {
            Guard.IsFalse(string.IsNullOrEmpty(celScriptName));

            try
            {
                var sourceFile = Path.Combine(applicationFolder, $"{celScriptName}.cs");

                var bodyLines = GetBodyLines(celScriptName, cels);

                var bodyText = FormatBodyText(bodyLines);

                var directory = Path.GetDirectoryName(sourceFile);
                Guard.IsNotNull(directory);

                Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(sourceFile, bodyText);
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Failed to generate source file. {ex.Message}");
            }

            return new SuccessResult();
        }

        private List<string> GetBodyLines(string celScriptName, List<ICelScriptNode> cels)
        {
            var body = new List<string>();
            body.Add("namespace CelApplication;");
            body.Add(string.Empty);
            body.Add($"public static class {celScriptName}");
            body.Add("{");

            bool first = true;
            foreach (var celScriptNode in cels)
            {
                var cel = celScriptNode as ICel;
                Guard.IsNotNull(cel);

                var celName = cel.Name;
                var returnType = GetReturnType(cel.Output);
                var parameters = GetParameters(cel.Input);

                if (first)
                {
                    first = false;
                }
                else
                {
                    body.Add(string.Empty);
                }

                body.Add($"public static {returnType} {celName}({parameters})");
                body.Add("{");

                var functionBodyLines = GetFunctionBodyLines(cel.Instructions, returnType);
                foreach (var functionBodyLine in functionBodyLines)
                {
                    body.Add(functionBodyLine);
                }

                body.Add("}");
            }

            body.Add("}");

            return body;
        }

        private string GetParameters(List<InstructionLine> outputInstructionLines)
        { 
            var sb = new StringBuilder();
            var first = true;
            foreach (var instructionLine in outputInstructionLines)
            {
                Guard.IsNotNull(instructionLine);

                var instruction = instructionLine.Instruction;
                Guard.IsNotNull(instruction);

                var typeInstruction = instruction as ITypeInstruction;
                Guard.IsNotNull(typeInstruction);

                var paramerName = typeInstruction.Name;
                var parameterType = ConvertType(typeInstruction);
                Guard.IsFalse(parameterType == "void");

                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append($"{parameterType} {paramerName}");
            }

            return sb.ToString();
        }

        private string ConvertType(ITypeInstruction instruction)
        {
            switch (instruction)
            {
                case PrimitivesMixin.Number _:
                    return "double";
                case PrimitivesMixin.Boolean _:
                    return "bool";
                case PrimitivesMixin.String _:
                    return "string";
            }

            return "void";
        }

        private string GetReturnType(List<InstructionLine> outputInstructionLines)
        {
            Guard.IsNotNull(outputInstructionLines);

            if (outputInstructionLines.Count == 1)
            {
                var typeInstruction = outputInstructionLines[0].Instruction as ITypeInstruction;
                Guard.IsNotNull(typeInstruction);

                return ConvertType(typeInstruction);
            }

            return "void";
        }

        private List<string> GetFunctionBodyLines(List<InstructionLine> instructionsLines, string returnType)
        {
            var body = new List<string>();

            for (int i = 0; i < instructionsLines.Count; i++)
            {
                InstructionLine instructionLine = instructionsLines[i];
                var instruction = instructionLine.Instruction;
                Guard.IsNotNull(instruction);

                IInstruction? nextInstruction = null;
                if (i < instructionsLines.Count - 1)
                {
                    nextInstruction = instructionsLines[i + 1].Instruction;
                }

                if (instruction is ITypeInstruction typeInstruction)
                {
                    var lhsType = ConvertType(typeInstruction);
                    if (instruction.PipeState == PipeState.NotConnected)
                    {
                        var expression = typeInstruction.GetExpression();
                        var expressionValue = expression.GetSummary(PropertyContext.CelInstructions);

                        // Bit of a hacky fix for fixing boolean case mismatch
                        // Todo: Fix this properly
                        if (expressionValue.Equals("True"))
                        {
                            expressionValue = "true";
                        }
                        if (expressionValue.Equals("False"))
                        {
                            expressionValue = "false";
                        }

                        body.Add($"{lhsType} {typeInstruction.Name} = {expressionValue};");
                    }
                    else
                    {
                        if (nextInstruction is BasicMixin.Call call)
                        {
                            var functionCall = GetFunctionCall(call);
                            body.Add($"{lhsType} {typeInstruction.Name} = {functionCall};");

                            i++; // Skip the next instruction
                        }
                        else if (nextInstruction is FileMixin.Read read)
                        {
                            body.Add($"{lhsType} {typeInstruction.Name} = \"{read.Resource}\";");

                            i++; // Skip the next instruction
                        }
                    }
                } 
                else if (instruction is BasicMixin.Call call)
                {
                    var functionCall = GetFunctionCall(call);
                    body.Add($"{functionCall};");
                }
                else if (instruction is BasicMixin.Return @return)
                {
                    if (string.IsNullOrEmpty(returnType))
                    {
                        body.Add($"return;");
                    }
                    else
                    {
                        var expression = @return.Result;
                        body.Add($"return {expression.Expression};");
                    }
                }
                else if (instruction is BasicMixin.Print print)
                {
                    var text = print.Message.GetSummary(PropertyContext.CelInstructions);
                    text = GetInterpolatedString(text);
                    body.Add($"Environment.Print({text});");
                }
                else if (instruction is BasicMixin.If @if)
                {
                    var condition = @if.Condition;
                    body.Add($"if ({condition.Expression})");
                    body.Add("{");
                }
                else if (instruction is BasicMixin.Else _)
                {
                    body.Add("}");
                    body.Add("else");
                    body.Add("{");
                }
                else if (instruction is BasicMixin.End _)
                {
                    body.Add("}");
                }
            }

            return body;
        }

        private string GetFunctionCall(BasicMixin.Call call)
        {
            Guard.IsNotNull(call);

            var functionArguments = GetFunctionArguments(call.Arguments);
            var functionCall = $"{call.Arguments.CelName}({functionArguments})";

            return functionCall;
        }

        private string GetFunctionArguments(CallArguments arguments)
        {
            var celSignature = arguments.CelSignature;

            // Use reflection to get information about its public members
            Type sigType = celSignature.GetType();

            bool first = true;

            var sb = new StringBuilder();
            foreach (var propertyInfo in sigType.GetProperties())
            {
                if (typeof(ExpressionBase).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    var expressionMember = propertyInfo.GetValue(celSignature) as ExpressionBase;
                    Guard.IsNotNull(expressionMember);

                    var memberName = propertyInfo.Name;

                    // Using GetSummary here because it correctly formats StringExpression with quotes
                    // Todo: We should probably just get the expression value and apply formatting logic here instead?
                    var memberValue = expressionMember.GetSummary(PropertyContext.CelInstructions);

                    if (expressionMember is StringExpression)
                    {
                        // Assume this is an interpolated string
                        memberValue = GetInterpolatedString(memberValue);
                    }

                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }                    
                    
                    sb.Append($"{memberName}: {memberValue}");
                }
            }

            return sb.ToString();
        }

        string GetInterpolatedString(string input)
        {
            if (input.StartsWith("\"") &&
                input.Contains("{") &&
                input.Contains("}") &&
                input.EndsWith("\""))
            {
                return "$" + input;
            }
            return input;
        }

        private string FormatBodyText(List<string> bodyLines)
        {
            var indent = 0;

            var sb = new StringBuilder();
            foreach (var line in bodyLines)
            {
                if (line.StartsWith("}"))
                {
                    indent--;
                }

                var whitespace = new string(' ', indent * 4);
                sb.Append(whitespace);
                sb.AppendLine(line);

                if (line.StartsWith("{"))
                {
                    indent++;
                }
            }

            return sb.ToString();
        }

        private async Task<Result> GenerateEnvironmentFile(string applicationFolder)
        {
            try
            {
                var sourceFile = Path.Combine(applicationFolder, $"Environment.cs");

                var bodyText =
@"using System;

namespace CelApplication;

public static class Environment
{
    public static Action<string> OnPrint;

    public static void Print(string message)
    {
        OnPrint?.Invoke(message);
    }
}
";
                var directory = Path.GetDirectoryName(sourceFile);
                Guard.IsNotNull(directory);

                Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(sourceFile, bodyText);
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Failed to generate source file. {ex.Message}");
            }

            return new SuccessResult();
        }
    }
}