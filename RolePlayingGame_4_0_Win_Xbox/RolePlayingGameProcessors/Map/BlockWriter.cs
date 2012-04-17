#region File Description
//-----------------------------------------------------------------------------
// BlockWriter.cs
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
    public class BlockWriter : RolePlayingGameWriter<Block>
    {
        WorldObjectWriter worldObjectWriter = null;

        protected override void Initialize(ContentCompiler compiler)
        {
            worldObjectWriter = compiler.GetTypeWriter(typeof(WorldObject)) 
                as WorldObjectWriter;

            base.Initialize(compiler);
        }

        protected override void Write(ContentWriter output, Block value)
        {
            // write out the base type
            output.WriteRawObject<WorldObject>(value as WorldObject, worldObjectWriter);

            // write out the Block data
            //output.Write(value.MoveType);
            output.Write(value.TextureName);
        }
    }
}
