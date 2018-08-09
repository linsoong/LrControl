using RtMidi.Core.Messages;

namespace LrControl.Core.Midi.Messages
{
    internal class NoteOffMessageHolder : MessageHolder<NoteOffMessage>
    {
        public NoteOffMessageHolder(in NoteOffMessage msg) : base(in msg)
        {
        }

        protected override bool CalculateHasChanged()
        {
            return LastSent.Channel != Message.Channel
                   || LastSent.Key != Message.Key
                   || LastSent.Velocity != Message.Velocity;
        }

        protected override void SendMessage(IMidiInputDeviceEventDispatcher dispatcher, in NoteOffMessage msg)
        {
            dispatcher.OnNoteOff(in msg);
        }
    }
}