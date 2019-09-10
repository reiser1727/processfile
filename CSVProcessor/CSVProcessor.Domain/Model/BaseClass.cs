namespace CSVProcessor.Domain
{
    public abstract class BaseClass
    {

        public string TableName { get; }

        public string[] Parameters { get; }
        
        public BaseClass()
        {
            this.TableName = nameof(BaseClass);
            this.Parameters = new string[] { };
        }

        public BaseClass(string tableName, string[] parameters)
        {
            this.TableName = tableName;
            this.Parameters = parameters;
        }

    }
}
