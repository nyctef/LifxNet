namespace LifxNet
{
    public class LightState
    {
        internal LightState(LightStateResponse state)
        {
            // TODO: translate into degrees/percentages
            Hue = state.Hue;
            Saturation = state.Saturation;
            Brightness = state.Brightness;
            Kelvin = state.Kelvin;
            IsOn = state.IsOn;
            Label = state.Label;
        }

        public ushort Hue { get; }
        public ushort Saturation { get; }
        public ushort Brightness { get; }
        public ushort Kelvin { get; }
        public bool IsOn { get; }
        public string Label { get; }
    }
}