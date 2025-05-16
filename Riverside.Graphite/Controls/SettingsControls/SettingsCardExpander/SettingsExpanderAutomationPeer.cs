using Microsoft.UI.Xaml.Automation.Peers;

namespace Graphite.Controls.SettingsControls
{
	public class SettingsExpanderAutomationPeer : FrameworkElementAutomationPeer
	{
		private readonly SettingsExpander _owner;

		public SettingsExpanderAutomationPeer(SettingsExpander owner) : base(owner)
		{
			_owner = owner;
		}

		protected override string GetClassNameCore()
		{
			return nameof(SettingsExpander);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}

		protected override string GetNameCore()
		{
			return _owner.Header ?? base.GetNameCore();
		}
	}
}

