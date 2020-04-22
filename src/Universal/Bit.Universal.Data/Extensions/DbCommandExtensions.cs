namespace System.Data.Common
{
    public static class DbCommandExtensions
    {
        public static void AddParameterWithValue(this DbCommand command, string parameterName, object parameterValue)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (parameterName == null)
                throw new ArgumentNullException(nameof(parameterName));
            if (parameterValue == null)
                throw new ArgumentNullException(nameof(parameterValue));

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
        }
    }
}
