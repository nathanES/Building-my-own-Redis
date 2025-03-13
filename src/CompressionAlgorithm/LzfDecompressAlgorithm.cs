namespace codecrafters_redis.CompressionAlgorithm;

public static class LzfDecompressAlgorithm
{
    public static int Decompress(Stream input, Stream output, int expectedSize)
    {
        int inputLength = (int)input.Length;
        byte[] inputData = new byte[inputLength];
        input.ReadExactly(inputData, 0, inputLength);

        byte[] outputData = new byte[expectedSize];

        int inputIndex = 0, outputIndex = 0;

        while (inputIndex < inputLength && outputIndex < expectedSize)
        {
            byte control = inputData[inputIndex++];

            if (control < 32) // Literal bytes
            {
                int length = control + 1;
                if (outputIndex + length > expectedSize) return -1;
                if (inputIndex + length > inputLength) return -1;

                Array.Copy(inputData, inputIndex, outputData, outputIndex, length);
                inputIndex += length;
                outputIndex += length;
            }
            else // Compressed sequence
            {
                int length = (control >> 5) + 2;
                int offset = ((control & 0x1F) << 8) + inputData[inputIndex++];

                if (outputIndex - offset < 0) return -1;
                if (outputIndex + length > expectedSize) return -1;

                for (int i = 0; i < length; i++)
                {
                    outputData[outputIndex] = outputData[outputIndex - offset];
                    outputIndex++;
                }
            }
        }

        output.Write(outputData, 0, outputIndex);
        return outputIndex;
    }
}