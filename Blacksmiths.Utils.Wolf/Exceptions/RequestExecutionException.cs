using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Exceptions
{
    public abstract class DbExecutionException : WolfException
    {
        public System.Data.Common.DbException DbException => this.InnerException as System.Data.Common.DbException;

        public DbExecutionException(Exception ex)
            : base(ex.Message, ex)
        {
        }
    }
    public class RequestExecutionException : DbExecutionException
    {
        public DataRequest Request { get; private set; }
        public Utility.WolfCommandBinding CommandBinding { get; set; }
        public RequestExecutionException(DataRequest request, Exception ex)
            : base(ex)
        {
            this.Request = request;
        }
    }

    public class CommitExecutionException : DbExecutionException
    {
        public ModelProcessor ModelProcessor { get; private set; }

        public CommitExecutionException(ModelProcessor modelProcessor, Exception ex)
            : base(ex)
        {
            this.ModelProcessor = modelProcessor;
        }
    }
}
