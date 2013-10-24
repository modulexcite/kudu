﻿namespace Kudu.Core.Jobs
{
    public interface IJobLogger
    {
        void ReportStatus<TJobStatus>(TJobStatus status) where TJobStatus : class, IJobStatus;

        void LogError(string error);

        void LogWarning(string warning);

        void LogInformation(string message);

        bool LogStandardOutput(string message);

        bool LogStandardError(string message);
    }
}