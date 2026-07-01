const formatter = new Intl.NumberFormat("en-MY", { style: "currency", currency: "MYR" });

export function formatCurrency(amount: number): string {
  return formatter.format(amount);
}
