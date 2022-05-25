﻿// 
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings;
using LVD.Stakhanovise.NET.Samples.FileHasher.FileGenerator;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Logging.Log4NetLogging;

namespace LVD.Stakhanovise.NET.Samples.FileHasher.FileProcessor
{
	public class StakhanoviseSourceFileProcessor : ISourceFileProcessor
	{
		private Stakhanovise mStakhanovise;

		private ISourceFileScheduler mSourceFileScheduler;

		public StakhanoviseSourceFileProcessor( ISourceFileScheduler sourceFileScheduler )
		{
			mSourceFileScheduler = sourceFileScheduler
				?? throw new ArgumentNullException( nameof( sourceFileScheduler ) );
		}

		public async Task StartProcesingFilesAsync( ISourceFileRepository sourceFileRepository,
			IFileHashRepository fileHashRepository,
			IProcessingWatcher processingWatcher )
		{
			mStakhanovise = await Stakhanovise
				.CreateForTheMotherland( new NetCoreConfigurationStakhanoviseDefaultsProvider() )
				.SetupWorkingPeoplesCommittee( setup =>
				{
					setup.WithLog4NetLogging();
					setup.SetupBuiltInTaskExecutorRegistryDependencies( depSetup =>
					{
						depSetup.BindToInstance<IFileHashRepository>( fileHashRepository );
						depSetup.BindToInstance<ISourceFileRepository>( sourceFileRepository );
						depSetup.BindToInstance<IProcessingWatcher>( processingWatcher );
					} );
				} )
				.StartFulfillingFiveYearPlanAsync();

			await mSourceFileScheduler.ScheduleFilesAsync( sourceFileRepository,
				processingWatcher );
		}

		public async Task StopProcessingFilesAsync()
		{
			await mStakhanovise.StopFulfillingFiveYearPlanAsync();
		}
	}
}
