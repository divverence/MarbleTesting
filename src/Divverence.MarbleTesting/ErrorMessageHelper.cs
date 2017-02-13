using System;

namespace Divverence.MarbleTesting
{
    internal static class ErrorMessageHelper
    {
        internal const string UpArrow = "\u2191";

        internal static string SequenceWithPointerToOffendingMoment(string sequence, int offensiveMoment)
        {
            return $"{Environment.NewLine}  {sequence}{Environment.NewLine}  {UpArrow.PadLeft(offensiveMoment + 1)}";
        }
    }
}