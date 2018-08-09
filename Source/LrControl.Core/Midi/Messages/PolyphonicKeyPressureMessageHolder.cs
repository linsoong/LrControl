using RtMidi.Core.Messages;

namespace LrControl.Core.Midi.Messages
{
    internal class PolyphonicKeyPressureMessageHolder : MessageHolder<PolyphonicKeyPressureMessage>
    {
        public PolyphonicKeyPressureMessageHolder(in PolyphonicKeyPressureMessage msg) : base(in msg)
        {
        }

        protected override bool CalculateHasChanged()
        {
            return LastSent.Channel != Message.Channel
                   || LastSent.Key != Message.Key
                   || LastSent.Pressure != Message.Pressure;
        }

        protected override void SendMessage(IMidiInputDeviceEventDispatcher dispatcher, in PolyphonicKeyPressureMessage msg)
        {
            dispatcher.OnPolyphonicKeyPressure(in msg);
        }
    }
}