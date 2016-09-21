﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Purpose:
// Custom exception to throw when the MultiShardDataReader is closed and
// the user attempts to perform some operation.
//
// Notes:

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.SqlDatabase.ElasticScale.Query
{
    // Suppression rationale: "Multi" is the spelling we want here.
    //
    /// <summary>
    /// Custom exception to throw when the <see cref="MultiShardDataReader"/> is closed and
    /// the user attempts to perform an operation on the closed reader. 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi"), Serializable]
    public class MultiShardDataReaderClosedException : Exception
    {
        #region Standard Exception Constructors

        /// <summary>
        /// Initializes a new instance of the MultiShardReaderClosedException class with a specified error message and a 
        /// reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference 
        /// if no inner exception is specified.
        /// </param>
        public MultiShardDataReaderClosedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultiShardDataReaderClosedException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MultiShardDataReaderClosedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultiShardDataReaderClosedException class.
        /// </summary>
        public MultiShardDataReaderClosedException()
            : base()
        {
        }

#if NET40

        /// <summary>
        /// Initializes a new instance of the MultiShardDataReaderClosedException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The SerializationInfo that holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The StreamingContext that contains contextual information about the source or destination.
        /// </param>
        protected MultiShardDataReaderClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif

        #endregion Standard Exception Constructors
    }
}
