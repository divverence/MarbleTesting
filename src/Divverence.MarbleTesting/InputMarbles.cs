using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Divverence.MarbleTesting
{
    public class InputMarbles
    {
        public InputMarbles(string sequence, IEnumerable<InputMarble> actions)
        {
            Sequence = sequence;
            Actions = actions.ToImmutableList();
        }

        public string Sequence { get; }
        public ImmutableList<InputMarble> Actions { get; }

        public Task Run(int time)
        {
            return Task.WhenAll(Actions.Where(e => e.Time == time).Select(exp => Run(exp, time)));
        }

        private async Task Run(InputMarble m, int time)
        {
            try
            {
                await m.Action();
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error when firing marble '{m.Marble}' at time {time} on sequence {ErrorMessageHelper.SequenceWithPointerToOffendingMoment(Sequence, time)}", e);
            }
        }
    }
}