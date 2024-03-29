﻿using System;

namespace LVD.Stakhanovise.NET.Options
{
	public class TaskQueueListenerOptions
	{
		public TaskQueueListenerOptions( string signalingConnectionString, string newTaskNotificationChannelName )
		{
			if ( string.IsNullOrEmpty( signalingConnectionString ) )
				throw new ArgumentNullException( nameof( signalingConnectionString ) );

			if ( string.IsNullOrEmpty( newTaskNotificationChannelName ) )
				throw new ArgumentNullException( nameof( newTaskNotificationChannelName ) );

			SignalingConnectionString = signalingConnectionString;
			NewTaskNotificationChannelName = newTaskNotificationChannelName;
			WaitNotificationTimeout = 250;
		}

		public string SignalingConnectionString
		{
			get; private set;
		}

		public string NewTaskNotificationChannelName
		{
			get; private set;
		}

		public int WaitNotificationTimeout
		{
			get; private set;
		}
	}
}
