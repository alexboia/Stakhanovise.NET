// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class DbScriptAssetSetup : ISetupDbAsset
	{
		private ISqlSetupScriptProvider mScriptProvider;

		private QueuedTaskMapping mDefaultMapping;

		public DbScriptAssetSetup( ISqlSetupScriptProvider scriptProvider )
		{
			if ( scriptProvider == null )
				throw new ArgumentNullException( nameof( scriptProvider ) );

			mScriptProvider = scriptProvider;
			mDefaultMapping = QueuedTaskMapping.Default;
		}

		public async Task SetupDbAssetAsync( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			if ( queueConnectionOptions == null )
				throw new ArgumentNullException( nameof( queueConnectionOptions ) );

			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );

			string scriptContents = await ReadScriptContentsAsync();
			if ( string.IsNullOrWhiteSpace( scriptContents ) )
				throw new InvalidDataException( "Script file is empty" );

			scriptContents = ProcessScriptContents( scriptContents,
				mapping );

			using ( NpgsqlConnection conn = await queueConnectionOptions.TryOpenConnectionAsync() )
			{
				using ( NpgsqlCommand cmdScript = new NpgsqlCommand( scriptContents, conn ) )
					await cmdScript.ExecuteNonQueryAsync();
			}
		}

		private async Task<string> ReadScriptContentsAsync()
		{
			return ( await mScriptProvider.GetScriptContentsAsync() )
				?.Trim();
		}

		private string ProcessScriptContents( string scriptContents, QueuedTaskMapping mapping )
		{
			string processedScriptContents = scriptContents.Trim();

			if ( mapping.QueueTableName != mDefaultMapping.QueueTableName )
				processedScriptContents = processedScriptContents.Replace( mDefaultMapping.QueueTableName,
					mapping.QueueTableName );

			if ( mapping.ResultsQueueTableName != mDefaultMapping.ResultsQueueTableName )
				processedScriptContents = processedScriptContents.Replace( mDefaultMapping.ResultsQueueTableName,
					mapping.ResultsQueueTableName );

			if ( mapping.ExecutionTimeStatsTableName != mDefaultMapping.ExecutionTimeStatsTableName )
				processedScriptContents = processedScriptContents.Replace( mDefaultMapping.ExecutionTimeStatsTableName,
					mapping.ExecutionTimeStatsTableName );

			if ( mapping.DequeueFunctionName != mDefaultMapping.DequeueFunctionName )
				processedScriptContents = processedScriptContents.Replace( mDefaultMapping.DequeueFunctionName,
					mapping.DequeueFunctionName );

			if ( mapping.MetricsTableName != mDefaultMapping.MetricsTableName )
				processedScriptContents = processedScriptContents.Replace( mDefaultMapping.MetricsTableName,
					mapping.MetricsTableName );

			return processedScriptContents;
		}
	}
}
