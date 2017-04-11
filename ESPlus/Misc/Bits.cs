namespace ESPlus
{
    public class Bits
    {
        public static int HexRequiredForAmountOfVariants(int parts)
        {
            var factor = 16;
            var sum = factor;
            var iterations = 1;

            while (sum < parts)
            {
                sum *= factor;
                ++iterations;
            }

            return iterations;
        }
    }
}
