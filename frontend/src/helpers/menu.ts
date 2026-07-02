import { MENU_ITEMS, MenuItemTypes } from "../constants/menu";

const getMenuItems = () => {
  // NOTE - You can fetch from server and return here as well
  return MENU_ITEMS;
}

/** Drops any item (and its children) whose `module` isn't in the caller's enabledModules — see docs/16_Feature_Flags.md §16.2. */
const filterMenuByModules = (menuItems: MenuItemTypes[], enabledModules: string[]): MenuItemTypes[] =>
  menuItems
    .filter((item) => !item.module || enabledModules.includes(item.module))
    .map((item) => (item.children ? { ...item, children: filterMenuByModules(item.children, enabledModules) } : item));

/** Drops any item (and its children) whose `roles` doesn't include one of the caller's roles. */
const filterMenuByRoles = (menuItems: MenuItemTypes[], userRoles: string[]): MenuItemTypes[] =>
  menuItems
    .filter((item) => !item.roles || item.roles.some((r) => userRoles.includes(r)))
    .map((item) => (item.children ? { ...item, children: filterMenuByRoles(item.children, userRoles) } : item));

const findAllParent = (
  menuItems: MenuItemTypes[],
  menuItem: MenuItemTypes
): string[] => {
  let parents: string[] = [];
  const parent = findMenuItem(menuItems, menuItem.parentKey);

  if (parent) {
    parents.push(parent.key);
    if (parent.parentKey) {
      parents = [...parents, ...findAllParent(menuItems, parent)];
    }
  }
  return parents;
}

const findMenuItem = (
  menuItems: MenuItemTypes[] | undefined,
  menuItemKey: MenuItemTypes['key'] | undefined
): MenuItemTypes | null => {
  if (menuItems && menuItemKey) {
    for (let i = 0; i < menuItems.length; i++) {
      if (menuItems[i].key === menuItemKey) {
        return menuItems[i];
      }
      const found = findMenuItem(menuItems[i].children, menuItemKey);
      if (found) return found;
    }
  }
  return null;
}

export { getMenuItems, filterMenuByModules, filterMenuByRoles, findAllParent, findMenuItem, };