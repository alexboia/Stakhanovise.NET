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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public class QueueTableDbAssetSetup : ISetupDbAsset
	{
		private ISetupDbAsset mTableScriptAssetSetup;

		private ISetupDbAsset mSequenceScriptAssetSetup;

		public QueueTableDbAssetSetup()
		{
			mSequenceScriptAssetSetup = new DbScriptAssetSetup(
				new EmbeddedResourceSqlSetupScriptProvider(
					GetType().Assembly,
					$"{GetType().Namespace}.BuiltInDbAssetsSetup.Scripts.sk_tasks_queue_t_task_lock_handle_id_seq.sql"
				)
			);

			mTableScriptAssetSetup = new DbScriptAssetSetup(
				new EmbeddedResourceSqlSetupScriptProvider(
					GetType().Assembly,
					$"{GetType().Namespace}.BuiltInDbAssetsSetup.Scripts.sk_tasks_queue_t.sql"
				)
			);
		}

		public async Task SetupDbAssetAsync( ConnectionOptions queueConnectionOptions, QueuedTaskMapping mapping )
		{
			await mSequenceScriptAssetSetup.SetupDbAssetAsync( queueConnectionOptions,
				mapping );
			await mTableScriptAssetSetup.SetupDbAssetAsync( queueConnectionOptions,
				mapping );
		}
	}
}
