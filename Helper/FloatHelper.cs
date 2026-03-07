public static class FloatHelper
{
    public static float Clamp(this float value, float min, float max)
    {
        if (value > max) return max;
        if (value < min) return min;
        return value;
    }
}
