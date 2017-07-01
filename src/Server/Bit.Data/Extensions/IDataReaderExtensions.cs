using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public static class IDataReaderExtensions
    {
        public static async Task PopulateStreamAsync(this IDataReader reader, Stream stream, CancellationToken cancellationToken)
        {
            await FlushSqlResultsToStream((DbDataReader)reader, stream, cancellationToken).ConfigureAwait(false);
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
                            throw new ArgumentException("SELECT query should not have " + reader.FieldCount + " columns (expected 1).", "reader");

                        string buffer = null;
                        if (reader[0].GetType().Name == "String")
                        {
                            buffer = reader.GetString(0);
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
                byte[] binary = Encoding.UTF8.GetBytes(content as string);
                await stream.WriteAsync(binary, 0, binary.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
