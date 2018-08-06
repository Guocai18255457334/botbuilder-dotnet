﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class ComponentDialog : Dialog
    {
        private DialogSet _dialogs;

        public ComponentDialog(string dialogId)
            : base(dialogId)
        {
            if (string.IsNullOrEmpty(dialogId))
            {
                throw new ArgumentNullException(nameof(dialogId));
            }

            _dialogs = new DialogSet();
        }

        public override async Task<DialogTurnResult> DialogBeginAsync(DialogContext dc, DialogOptions options = null)
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Start the inner dialog.
            var cdc = new DialogContext(_dialogs, dc.Context, dc.ActiveDialog.State);
            var turnResult = await OnDialogBeginAsync(cdc, options).ConfigureAwait(false);

            // Check for end of inner dialog 
            if (turnResult.HasResult)
            {
                // Return result to calling dialog
                return await dc.EndAsync(turnResult.Result).ConfigureAwait(false);
            }
            else
            {
                // Just signal end of turn
                return Dialog.EndOfTurn;
            }
        }

        public async Task<DialogTurnResult> DialogContinueAsync(DialogContext dc)
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Continue execution of inner dialog.
            var cdc = new DialogContext(_dialogs, dc.Context, dc.ActiveDialog.State);
            var turnResult = await OnDialogContinueAsync(cdc).ConfigureAwait(false);

            // Check for end of inner dialog 
            if (turnResult.HasResult)
            {
                // Return result to calling dialog
                return await dc.EndAsync(turnResult.Result).ConfigureAwait(false);
            }
            else
            {
                // Just signal end of turn
                return Dialog.EndOfTurn;
            }
        }

        public override async Task<DialogTurnResult> DialogResumeAsync(DialogContext dc, DialogReason reason, object result = null)
        {
            // Containers are typically leaf nodes on the stack but the dev is free to push other dialogs
            // on top of the stack which will result in the container receiving an unexpected call to
            // dialogResume() when the pushed on dialog ends. 
            // To avoid the container prematurely ending we need to implement this method and simply 
            // ask our inner dialog stack to re-prompt.
            await DialogRepromptAsync(dc.Context, dc.ActiveDialog).ConfigureAwait(false);
            return Dialog.EndOfTurn;
        }

        public override async Task DialogRepromptAsync(ITurnContext context, DialogInstance instance)
        {
            // Delegate to inner dialog.
            var cdc = new DialogContext(_dialogs, context, instance.State);
            await OnDialogRepromptAsync(cdc).ConfigureAwait(false);
        }

        public override async Task DialogEndAsync(ITurnContext context, DialogInstance instance, DialogReason reason)
        {
            // Notify inner dialog
            var cdc = new DialogContext(_dialogs, context, instance.State);
            await OnDialogEndAsync(cdc, reason).ConfigureAwait(false);
        }

        protected string InitialDialogId { get; set; }

        protected Dialog AddDialog(Dialog dialog)
        {
            _dialogs.Add(dialog);
            if (string.IsNullOrEmpty(InitialDialogId))
            {
                InitialDialogId = dialog.Id;
            }
            return dialog;
        }


        protected async Task<DialogTurnResult> OnDialogBeginAsync(DialogContext dc, DialogOptions options)
        {
            return await dc.BeginAsync(InitialDialogId, options).ConfigureAwait(false);
        }

        protected async Task OnDialogEndAsync(DialogContext dc, DialogReason reason)
        {
            if (reason == DialogReason.CancelCalled) {
                await dc.CancelAllAsync().ConfigureAwait(false);
            }
        }

        protected async Task<DialogTurnResult> OnDialogContinueAsync(DialogContext dc)
        {
            return await dc.ContinueAsync().ConfigureAwait(false);
        }

        protected async Task OnDialogRepromptAsync(DialogContext dc)
        {
            await dc.RepromptAsync().ConfigureAwait(false);
        }
    }
}