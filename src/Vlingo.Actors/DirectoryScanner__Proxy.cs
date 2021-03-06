﻿// Copyright (c) 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Actors
{
    public class DirectoryScanner__Proxy : IDirectoryScanner
    {
        private const string ActorOfRepresentation1 = "ActorOf<T>(Vlingo.Actors.Address)";

        private readonly Actor actor;
        private readonly IMailbox mailbox;

        public DirectoryScanner__Proxy(Actor actor, IMailbox mailbox)
        {
            this.actor = actor;
            this.mailbox = mailbox;
        }

        public ICompletes<T> ActorOf<T>(Address address)
        {
            if (!actor.IsStopped)
            {
                Action<IDirectoryScanner> consumer = actor => actor.ActorOf<T>(address);
                var completes = new BasicCompletes<T>(actor.Scheduler);
                mailbox.Send(new LocalMessage<IDirectoryScanner>(actor, consumer, completes, ActorOfRepresentation1));
                return completes;
            }
            else
            {
                actor.DeadLetters.FailedDelivery(new DeadLetter(actor, ActorOfRepresentation1));
            }
            return null;
        }
    }
}
