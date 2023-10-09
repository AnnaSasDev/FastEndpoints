﻿namespace FastEndpoints;

public interface IJobStorageRecord
{
    /// <summary>
    /// a unique id for the job queue. each command type has it's own queue. this is automatically generated by the library.
    /// </summary>
    string QueueID { get; set; }

    /// <summary>
    /// the actual command object that will be embedded in the storage record.
    /// if your database/orm (such as ef-core) doesn't support embedding objects, you can take the following steps:
    /// <code>
    /// 1. add a [NotMapped] attribute to this property.
    /// 2. add a new property, either a <see langword="string" /> or <see cref="byte" /> array
    /// 3. implement both <see cref="GetCommand" /> and <see cref="SetCommand" /> to serialize/deserialize the command object back and forth and store it in the newly added property.
    /// </code>
    /// you may use any serializer you please. recommendation is to use MessagePack.
    /// </summary>
    object Command { get; set; }

    /// <summary>
    /// the job will not be executed before this date/time. by default it will automatically be set to the time of creation allowing jobs to be
    /// executed as soon as they're created.
    /// </summary>
    DateTime ExecuteAfter { get; set; }

    /// <summary>
    /// the expiration date/time of job. if the job remains in an incomplete state past this time, the record is considered stale.
    /// </summary>
    DateTime ExpireOn { get; set; }

    /// <summary>
    /// indicates whether the job has successfully completed or not.
    /// </summary>
    bool IsComplete { get; set; }

    /// <summary>
    /// implement this function to customize command deserialization.
    /// </summary>
    TCommand GetCommand<TCommand>() where TCommand : ICommand
        => (TCommand)Command;

    /// <summary>
    /// implement this method to customize command serialization.
    /// </summary>
    void SetCommand<TCommand>(TCommand command) where TCommand : ICommand
        => Command = command;
}