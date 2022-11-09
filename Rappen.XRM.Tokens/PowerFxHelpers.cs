/* Note!
 *
 * To use these, you need to reach these files:
 *
 * Microsoft.PowerFx.Core.dll                  0.2.3.0
 * Microsoft.PowerFx.Interpreter.dll           0.2.3.0
 * Microsoft.PowerFx.Transport.Attributes.dll  0.2.3.0
 * en-US\Microsoft.PowerFx.Core.resources.dll  0.2.3.0
 * System.Runtime.CompilerServices.Unsafe.dll  4.6.28619.1  For XrmToolBox might need ILMerge
 * System.Collections.Immutable.dll            5.0.20.51904
*/

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rappen.Power.Fx
{
    public class PowerFxHelpers
    {
        //private const bool FormatTable = false;
        private static RecalcEngine engine;

        /// <summary>
        /// Power Fx Eval method
        /// </summary>
        /// <remarks>Very inspired by https://github.com/microsoft/power-fx-host-samples/blob/84cf538bd7ea451c002c898e98cb6a3be344b5ff/Samples/ConsoleREPL/ConsoleREPL.cs#L82-L112</remarks>
        /// <param name="expr">Text to PowerFx.</param>
        /// <param name="engine">The engine used to Eval.</param>
        /// <returns>Text after Eval.</returns>
        /// <exception cref="InvalidPluginExecutionException">Any handled errors throws as InvalidPluginExecutionException.</exception>
        public static string Eval(string expr)
        {
            if (engine == null)
            {
                engine = new RecalcEngine();
            }
            Match match;
            if ((match = Regex.Match(expr, @"^\s*Set\(\s*(?<ident>\w+)\s*,\s*(?<expr>.*)\)", RegexOptions.Singleline)).Success)
            {
                var r = engine.Eval(match.Groups["expr"].Value);
                //   Console.WriteLine(match.Groups["ident"].Value + ": " + PrintResult(r));
                engine.UpdateVariable(match.Groups["ident"].Value, r);
                return PrintResult(r);
            }

            // formula definition: <ident> = <formula>
            else if ((match = Regex.Match(expr, @"^\s*(?<ident>\w+)\s*=(?<formula>.*)$", RegexOptions.Singleline)).Success)
                engine.SetFormula(match.Groups["ident"].Value, match.Groups["formula"].Value, null /*OnUpdate*/);

            // function definition: <ident>( <ident> : <type>, ... ) : <type> = <formula>
            //                      <ident>( <ident> : <type>, ... ) : <type> { <formula>; <formula>; ... }
            else if (Regex.IsMatch(expr, @"^\s*\w+\((\s*\w+\s*\:\s*\w+\s*,?)*\)\s*\:\s*\w+\s*(\=|\{).*$", RegexOptions.Singleline))
            {
                var res = engine.DefineFunctions(expr);
                if (res.Errors.Count() > 0)
                    throw new InvalidPluginExecutionException($"PowerFx: {expr} Error: {res.Errors.FirstOrDefault().Message}");
            }

            // eval and print everything else
            else
            {
                try
                {
                    var result = engine.Eval(expr);

                    if (result is ErrorValue errorValue)
                        throw new InvalidPluginExecutionException($"PowerFx: {expr} Error: {errorValue.Errors.FirstOrDefault().Message}");
                    else
                        return PrintResult(result);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw new InvalidPluginExecutionException($"PowerFx: {expr} Error: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            return string.Empty;
        }

        // Exact copy from https://github.com/microsoft/power-fx-host-samples/blob/84cf538bd7ea451c002c898e98cb6a3be344b5ff/Samples/ConsoleREPL/ConsoleREPL.cs#L221-L327
        private static string PrintResult(object value, Boolean minimal = false)
        {
            string resultString = "";

            if (value is BlankValue)
                resultString = (minimal ? "" : "Blank()");
            else if (value is ErrorValue errorValue)
                resultString = (minimal ? "<error>" : "<Error: " + errorValue.Errors[0].Message + ">");
            else if (value is UntypedObjectValue)
                resultString = (minimal ? "<untyped>" : "<Untyped: Use Value, Text, Boolean, or other functions to establish the type>");
            else if (value is StringValue str)
                resultString = (minimal ? str.ToObject().ToString() : "\"" + str.ToObject().ToString().Replace("\"", "\"\"") + "\"");
            else if (value is RecordValue record)
            {
                if (minimal)
                    resultString = "<record>";
                else
                {
                    var separator = "";
                    resultString = "{";
                    foreach (var field in record.Fields)
                    {
                        resultString += separator + $"{field.Name}:";
                        resultString += PrintResult(field.Value);
                        separator = ", ";
                    }
                    resultString += "}";
                }
            }
            else if (value is TableValue table)
            {
                if (minimal)
                    resultString = "<table>";
                else
                {
                    int[] columnWidth = new int[table.Rows.First().Value.Fields.Count()];

                    foreach (var row in table.Rows)
                    {
                        var column = 0;
                        foreach (var field in row.Value.Fields)
                        {
                            columnWidth[column] = Math.Max(columnWidth[column], PrintResult(field.Value, true).Length);
                            column++;
                        }
                    }

                    // special treatment for single column table named Value
                    if (columnWidth.Length == 1 && table.Rows.First().Value.Fields.First().Name == "Value")
                    {
                        string separator = "";
                        resultString = "[";
                        foreach (var row in table.Rows)
                        {
                            resultString += separator + PrintResult(row.Value.Fields.First().Value);
                            separator = ", ";
                        }
                        resultString += "]";
                    }
                    // otherwise a full table treatment is needed
                    //else if (FormatTable)
                    //{
                    //    resultString = "\n ";
                    //    var column = 0;
                    //    foreach (var field in table.Rows.First().Value.Fields)
                    //    {
                    //        columnWidth[column] = Math.Max(columnWidth[column], field.Name.Length);
                    //        resultString += " " + field.Name.PadLeft(columnWidth[column]) + "  ";
                    //        column++;
                    //    }
                    //    resultString += "\n ";
                    //    foreach (var width in columnWidth)
                    //        resultString += new string('=', width + 2) + " ";

                    //    foreach (var row in table.Rows)
                    //    {
                    //        column = 0;
                    //        resultString += "\n ";
                    //        foreach (var field in row.Value.Fields)
                    //        {
                    //            resultString += " " + PrintResult(field.Value, true).PadLeft(columnWidth[column]) + "  ";
                    //            column++;
                    //        }
                    //    }
                    //}
                    // table without formatting
                    else
                    {
                        resultString = "[";
                        string separator = "";
                        foreach (var row in table.Rows)
                        {
                            resultString += separator + PrintResult(row.Value);
                            separator = ", ";
                        }
                        resultString += "]";
                    }
                }
            }
            // must come last, as everything is a formula value
            else if (value is FormulaValue fv)
                resultString = fv.ToObject().ToString();
            else
                throw new Exception("unexpected type in PrintResult");

            return (resultString);
        }
    }
}