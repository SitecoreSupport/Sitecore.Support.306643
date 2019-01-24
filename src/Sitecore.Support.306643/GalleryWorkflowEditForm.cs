using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Shell.Framework.CommandBuilders;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Workflows;
using System;
using System.Web.UI;

namespace Sitecore.Shell.Applications.ContentManager.Galleries.WorkflowEdit
{
  /// <summary>Represents a menu as a gallery.</summary>
  public class GalleryWorkflowEditForm : GalleryForm
  {
    /// <summary>The m_check in item.</summary>
    private Item checkInItem;

    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    /// <value>The options.</value>
    protected Menu Options
    {
      get;
      set;
    }

    /// <summary>Handles the message.</summary>
    /// <param name="message">The message.</param>
    public override void HandleMessage(Message message)
    {
      Assert.ArgumentNotNull(message, "message");
      Invoke(message, true);
      message.CancelBubble = true;
      message.CancelDispatch = true;
    }

    /// <summary>Raises the load event.</summary>
    /// <param name="e">The <see cref="T:System.EventArgs" /> instance containing the event data.</param>
    /// <remarks>This method notifies the server control that it should perform actions common to each HTTP
    /// request for the page it is associated with, such as setting up a database query. At this
    /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
    /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
    /// property to determine whether the page is being loaded in response to a client postback,
    /// or if it is being loaded and accessed for the first time.</remarks>
    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
        if (itemFromQueryString != null && HasField(itemFromQueryString, FieldIDs.Workflow))
        {
          IWorkflow workflow;
          WorkflowState state;
          WorkflowCommand[] commands;
          GetCommands(itemFromQueryString, out workflow, out state, out commands);
          bool flag = IsCommandEnabled("item:checkout", itemFromQueryString);
          bool flag2 = IsCommandEnabled("item:checkin", itemFromQueryString);
          bool flag3 = false;
          if (commands != null)
          {
            flag3 = CanShowCommands(itemFromQueryString, commands);
          }
          RenderText(workflow, state, itemFromQueryString);
          if ((workflow != null && flag3) | flag | flag2)
          {
            if (flag)
            {
              RenderEdit();
            }
            if (Settings.Workflows.Enabled)
            {
              if (flag2)
              {
                RenderCheckIn(itemFromQueryString);
              }
              if (commands != null)
              {
                RenderCommands(workflow, itemFromQueryString, commands);
              }
            }
          }
        }
      }
    }

    /// <summary>Determines whether this instance can show commands.</summary>
    /// <param name="item">The item.</param>
    /// <param name="commands">The commands.</param>
    /// <returns><c>true</c> if this instance [can show commands] the specified item; otherwise, <c>false</c>.</returns>
    private static bool CanShowCommands(Item item, WorkflowCommand[] commands)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(commands, "commands");
      if (!item.Appearance.ReadOnly && commands.Length != 0)
      {
        if (!item.Locking.CanLock() && !item.Locking.HasLock())
        {
          return Context.IsAdministrator;
        }
        return true;
      }
      return false;
    }

    /// <summary>Gets the commands.</summary>
    /// <param name="item">The item.</param>
    /// <param name="workflow">The workflow.</param>
    /// <param name="state">The state.</param>
    /// <param name="commands">The commands.</param>
    private static void GetCommands(Item item, out IWorkflow workflow, out WorkflowState state, out WorkflowCommand[] commands)
    {
      Assert.ArgumentNotNull(item, "item");
      workflow = null;
      commands = null;
      state = null;
      IWorkflowProvider workflowProvider = Client.ContentDatabase.WorkflowProvider;
      if (workflowProvider != null && workflowProvider.GetWorkflows().Length != 0)
      {
        workflow = workflowProvider.GetWorkflow(item);
        if (workflow != null)
        {
          state = workflow.GetState(item);
          if (state != null)
          {
            commands = workflow.GetCommands(item);
            commands = WorkflowFilterer.FilterVisibleCommands(commands, item);
          }
        }
      }
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="workflow">The workflow.</param>
    /// <param name="state">The state.</param>
    /// <returns>The text.</returns>
    private static string GetText(Item item, IWorkflow workflow, WorkflowState state)
    {
      string str = string.Empty;
      if (item.Locking.IsLocked())
      {
        str = ((!item.Locking.HasLock()) ? Translate.Text("The item is locked by <b>{0}</b>.", item.Locking.GetOwnerWithoutDomain()) : Translate.Text("The item is locked by <b>you</b>."));
        str = "<br/><br/>" + str;
      }
      if (workflow == null)
      {
        return Translate.Text("The item is currently not part of a workflow.") + str;
      }
      string @string = StringUtil.GetString(workflow.Appearance.DisplayName, "?");
      if (state == null)
      {
        return Translate.Text("The item is part of the <b>{0}</b> workflow,<br/>but has no state.", @string) + str;
      }
      string string2 = StringUtil.GetString(state.DisplayName, "?");
      return Translate.Text("The item is in the <b>{0}</b> state<br/>in the <b>{1}</b> workflow.", string2, @string) + str;
    }

    /// <summary>Determines whether the specified item has field.</summary>
    /// <param name="item">The item.</param>
    /// <param name="fieldID">The field ID.</param>
    /// <returns><c>true</c> if the specified item has field; otherwise, <c>false</c>.</returns>
    private static bool HasField(Item item, ID fieldID)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(fieldID, "fieldID");
      return TemplateManager.IsFieldPartOfTemplate(fieldID, item);
    }

    /// <summary>Gets the check in item.</summary>
    /// <returns>The check in item.</returns>
    private Item GetCheckInItem()
    {
      if (checkInItem == null)
      {
        checkInItem = Client.CoreDatabase.GetItem("/sitecore/system/Settings/Workflow/Check In");
      }
      return checkInItem;
    }

    /// <summary>Determines whether [is command enabled] [the specified command].</summary>
    /// <param name="command">The command.</param>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if [is command enabled] [the specified command]; otherwise, <c>false</c>.</returns>
    private bool IsCommandEnabled(string command, Item item)
    {
      Assert.ArgumentNotNullOrEmpty(command, "command");
      Assert.ArgumentNotNull(item, "item");
      CommandState commandState = CommandManager.QueryState(command, item);
      if (commandState != CommandState.Down)
      {
        return commandState == CommandState.Enabled;
      }
      return true;
    }

    /// <summary>Renders the check in.</summary>
    /// <param name="item">The item.</param>
    private void RenderCheckIn(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      string header = Translate.Text("Check In");
      string icon = "Office/16x16/check.png";
      Item item2 = GetCheckInItem();
      if (item2 != null)
      {
        header = item2["Header"];
        icon = item2["Icon"];
      }
      MenuItem menuItem = new MenuItem();
      Options.Controls.Add(menuItem);
      menuItem.Header = header;
      menuItem.Icon = icon;
      menuItem.Click = "item:checkin";
    }

    /// <summary>Renders the commands.</summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="item">The item.</param>
    /// <param name="commands">The commands.</param>
    private void RenderCommands(IWorkflow workflow, Item item, WorkflowCommand[] commands)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(commands, "commands");
      foreach (WorkflowCommand workflowCommand in commands)
      {
        MenuItem menuItem = new MenuItem();
        Options.Controls.Add(menuItem);
        menuItem.Header = workflowCommand.DisplayName;
        menuItem.Icon = workflowCommand.Icon;
        menuItem.Click = new WorkflowCommandBuilder(item, workflow, workflowCommand).ToString();
        menuItem.Disabled = (!Context.User.IsAdministrator && !item.Locking.HasLock());
      }
    }

    /// <summary>Renders the edit.</summary>
    private void RenderEdit()
    {
      MenuItem menuItem = new MenuItem();
      Options.Controls.Add(menuItem);
      menuItem.Header = "Edit";
      menuItem.Icon = "Office/24x24/edit_in_workflow.png";
      menuItem.Click = "item:checkout";
    }

    /// <summary>
    /// Renders the text.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="state">The state.</param>
    /// <param name="item">The item.</param>
    private void RenderText(IWorkflow workflow, WorkflowState state, Item item)
    {
      MenuHeader menuHeader = new MenuHeader();
      Options.Controls.Add(menuHeader);
      menuHeader.Header = "Workflow";
      string text = GetText(item, workflow, state);
      MenuPanel menuPanel = new MenuPanel();
      Options.Controls.Add(menuPanel);
      menuPanel.Controls.Add(new LiteralControl("<div class=\"scMenuItem\">" + text + "</div>"));
    }
  }
}