﻿using System;
using System.Collections.Generic;
using RPGCore.Items;

namespace RPGCore.Inventory.Slots
{
	public class ItemSelectSlot : ItemSlot
	{
		internal Item SelectedItem;

		public override Item CurrentItem => SelectedItem;

		public override IEnumerable<Item> Items
		{
			get
			{
				if (SelectedItem == null)
				{
					yield break;
				}

				yield return SelectedItem;
			}
		}

		public override InventoryResult AddItem (Item item)
		{
			if (item is null)
			{
				throw new ArgumentNullException (nameof (item), "Cannot add \"null\" item to storage slot.");
			}

			SelectedItem = item;

			if (item is StackableItem stackableItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, stackableItem.Quantity);
			}
			else if (item is UniqueItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, 1);
			}
			else
			{
				throw new InvalidOperationException ($"Item in neither a {nameof (StackableItem)} nor a {nameof (UniqueItem)}.");
			}
		}

		public override InventoryResult MoveInto (Inventory other)
		{
			if (other is null)
			{
				throw new ArgumentNullException (nameof (other), "Cannot move into a \"null\" inventory.");
			}

			SelectedItem = null;

			return new InventoryResult (null, InventoryResult.OperationStatus.Complete, 0);
		}

		public override InventoryResult RemoveItem ()
		{
			SelectedItem = null;

			return new InventoryResult (null, InventoryResult.OperationStatus.Complete, 0);
		}

		public override InventoryResult SetItem (Item item)
		{
			if (item is null)
			{
				throw new ArgumentNullException (nameof (item), "Cannot add \"null\" item to storage slot.");
			}

			SelectedItem = item;

			if (item is StackableItem stackableItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, stackableItem.Quantity);
			}
			else if (item is UniqueItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, 1);
			}
			else
			{
				throw new InvalidOperationException ($"Item in neither a {nameof (StackableItem)} nor a {nameof (UniqueItem)}.");
			}
		}

		public override InventoryResult SwapInto (ItemSlot other)
		{
			if (other is null)
			{
				throw new ArgumentNullException (nameof (other), $"Cannot swap into a \"null\" {nameof (ItemSlot)}.");
			}

			if (other is ItemSelectSlot itemSelectSlot)
			{
				var temp = SelectedItem;
				SelectedItem = itemSelectSlot.SelectedItem;
				itemSelectSlot.SelectedItem = temp;
			}
			else if (other is ItemStorageSlot itemStorageSlot)
			{
				SelectedItem = itemStorageSlot.CurrentItem;
			}
			else
			{
				throw new InvalidOperationException ($"Slot in neither a {nameof (ItemStorageSlot)} nor a {nameof (ItemSelectSlot)}.");
			}

			if (SelectedItem == null)
			{
				return InventoryResult.None;
			}

			if (SelectedItem is StackableItem stackableItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, stackableItem.Quantity);
			}
			else if (SelectedItem is UniqueItem)
			{
				return new InventoryResult (null, InventoryResult.OperationStatus.Complete, 1);
			}
			else
			{
				throw new InvalidOperationException ($"Item in neither a {nameof (StackableItem)} nor a {nameof (UniqueItem)}.");
			}
		}
	}
}
