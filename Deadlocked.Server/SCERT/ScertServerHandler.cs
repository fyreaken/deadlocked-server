﻿using Deadlocked.Server.SCERT.Models.Packets;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Deadlocked.Server.SCERT
{
    public class ScertServerHandler : SimpleChannelInboundHandler<BaseScertMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ScertServerHandler>();

        public override bool IsSharable => true;

        public IChannelGroup Group = null;


        public Action<IChannel> OnChannelActive;
        public Action<IChannel> OnChannelInactive;
        public Action<IChannel, BaseScertMessage> OnChannelMessage;

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            IChannelGroup g = Group;
            if (g == null)
            {
                lock (this)
                {
                    if (Group == null)
                    {
                        Group = new DefaultChannelGroup(ctx.Executor);
                    }
                }
            }

            // Add to channels list
            g.Add(ctx.Channel);

            // Send event upstream
            OnChannelActive?.Invoke(ctx.Channel);
        }

        // The Channel is closed hence the connection is closed
        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            IChannelGroup g = Group;
            if (g == null)
            {
                lock (this)
                {
                    if (Group == null)
                    {
                        Group = new DefaultChannelGroup(ctx.Executor);
                    }
                }
            }

            Logger.Info("Client disconnected");

            // Remove
            g.Remove(ctx.Channel);

            // Send event upstream
            OnChannelInactive?.Invoke(ctx.Channel);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, BaseScertMessage message)
        {
            // Send upstream
            OnChannelMessage?.Invoke(ctx.Channel, message);
        }
    }
}
