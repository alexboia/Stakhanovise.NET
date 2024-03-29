﻿// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class QueuedTask : IQueuedTask, IEquatable<QueuedTask>
	{
		public QueuedTask()
		{
			return;
		}

		public QueuedTask( Guid taskId )
			: this()
		{
			Id = taskId;
		}

		public QueuedTask( IQueuedTask other )
			: this()
		{
			if ( other == null )
				throw new ArgumentNullException( nameof( other ) );

			Id = other.Id;
			LockHandleId = other.LockHandleId;
			Type = other.Type;
			Source = other.Source;
			Payload = other.Payload;
			Priority = other.Priority;
			LockedUntilTs = other.LockedUntilTs;
			PostedAtTs = other.PostedAtTs;
		}

		public bool Equals( QueuedTask other )
		{
			if ( other == null )
				return false;

			if ( Id.Equals( Guid.Empty ) && other.Id.Equals( Guid.Empty ) )
				return ReferenceEquals( this, other );

			return Id.Equals( other.Id );
		}

		public override bool Equals( object obj )
		{
			return Equals( obj as QueuedTask );
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public Guid Id
		{
			get; set;
		}

		public long LockHandleId
		{
			get; set;
		}

		public string Type
		{
			get; set;
		}

		public string Source
		{
			get; set;
		}

		public object Payload
		{
			get; set;
		}

		public int Priority
		{
			get; set;
		}

		public DateTimeOffset LockedUntilTs
		{
			get; set;
		}

		public DateTimeOffset PostedAtTs
		{
			get; set;
		}
	}
}
