using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public static class DbDataReaderExtensions
    {
        public static async Task PopulateStreamAsync(this DbDataReader reader, Stream stream, CancellationToken cancellationToken)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            await FlushSqlResultsToStream(reader, stream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task FlushSqlResultsToStream(DbDataReader reader, Stream stream, CancellationToken cancellationToken)
        {
            using (reader)
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (reader.HasRows)
                    {
                        if (reader.FieldCount != 1)
                            throw new ArgumentException($"SELECT query should not have {reader.FieldCount} columns (expected 1).", nameof(reader));

                        if (reader[0].GetType().Name == "String")
                        {
                            string buffer = reader.GetString(0);
                            await FlushContent(stream, buffer, -1, cancellationToken).ConfigureAwait(false);
                        }
                        else if (reader[0].GetType().Name == "Byte[]")
                        {
                            byte[] binary = new byte[2048];
                            int amount = (int)reader.GetBytes(0, 0, binary, 0, 2048);
                            int pos = amount;
                            do
                            {
                                await FlushContent(stream, binary, amount, cancellationToken).ConfigureAwait(false);
                                amount = (int)reader.GetBytes(0, pos, binary, 0, 2048);
                                pos += amount;
                            }
                            while (amount > 0);
                        }
                    }
                }
            }
        }

        private static async Task FlushContent(Stream stream, object content, int amount, CancellationToken cancellationToken)
        {
            if (amount > -1)
            {
                await stream.WriteAsync((content as byte[]), 0, amount, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                byte[] binary = Encoding.UTF8.GetBytes((string)content);
                await stream.WriteAsync(binary, 0, binary.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
