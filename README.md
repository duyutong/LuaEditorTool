# LuaEditorTool
利用lua的动态编译特性，在编辑器模式批量修改预制体信息

-- 这是一个替换预制体内所有Text组件字体的示例

local textComponents = prefab:GetComponentsInChildren(typeof(CS.UnityEngine.UI.Text))
local fontToReplace = asset

for i = 0, textComponents.Length - 1 do
	if textComponents[i].font:Equals(nil) or textComponents[i].font .name ~= fontToReplace.name then
		textComponents[i].font = fontToReplace
		checkDirty = true
	end
end
