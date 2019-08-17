namespace System.Data.Common
{
    public static class DbCommandExtensions
    {
        public static void AddParameterWithValue(this DbCommand command, string parameterName, object parameterValue)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
        }
    }
}
