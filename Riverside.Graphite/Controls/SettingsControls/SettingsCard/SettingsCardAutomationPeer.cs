using Microsoft.UI.Xaml.Automation.Peers;

namespace Graphite.Controls.SettingsControls
{
	public class SettingsCardAutomationPeer : FrameworkElementAutomationPeer
	{
		private readonly SettingsCard _owner;

		public SettingsCardAutomationPeer(SettingsCard owner)
			: base(owner)
		{
			_owner = owner;
		}

		protected override string GetClassNameCore()
		{
			return nameof(SettingsCard);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}

		protected override string GetLocalizedControlTypeCore()
		{
			return "settings card";
		}

		protected override string GetNameCore()
		{
			return _owner.Header ?? base.GetNameCore();
		}
	}
}

