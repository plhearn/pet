#region File Description
//-----------------------------------------------------------------------------
// CutsceneWriter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using RolePlayingGameData;
#endregion

namespace RolePlayingGameProcessors
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class CutsceneWriter : RolePlayingGameWriter<Cutscene>
    {
        WorldObjectWriter worldObjectWriter = null;

        protected override void Initialize(ContentCompiler compiler)
        {
            worldObjectWriter = compiler.GetTypeWriter(typeof(WorldObject))
                as WorldObjectWriter;

            base.Initialize(compiler);
        }

        protected override void Write(ContentWriter output, Cutscene value)
        {
            // remove any entries that have zero quantity
            value.Entries.RemoveAll(delegate(ContentEntry<Cutscene> contentEntry)
            {
                return (contentEntry.Count <= 0);
            });

            // write out the base type
            output.WriteRawObject<WorldObject>(value as WorldObject, worldObjectWriter);

            // write out the Cutscene data
            output.Write(value.sceneData);
            output.WriteObject(value.Entries);
            output.Write(value.Name);
        }
    }
}
