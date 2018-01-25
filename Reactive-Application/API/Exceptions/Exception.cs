using System;
namespace API.Exceptions
{
    /// <summary>
    ///  Exception claas to handle JobCanceledException raised by client executer
    /// </summary>
    public class JobCanceledException:Exception
    {
    }

    public class JobTimeOutException:Exception
    {
    }

    /// <summary>
    /// Exception claas to handle UnHandledException raised by client executer
    /// </summary>
    public class UnHandledException : Exception
    {
    }

    public class CoordinatorStoppedException : Exception
    {
    }
}
