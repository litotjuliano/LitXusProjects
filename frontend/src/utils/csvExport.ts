import { downloadBlob } from "./fileDownload";

function escapeCsvField(value: string | number): string {
  const str = String(value);
  return /[",\n]/.test(str) ? `"${str.replace(/"/g, '""')}"` : str;
}

export function downloadCsv(filename: string, rows: (string | number)[][]): void {
  const csv = rows.map((row) => row.map(escapeCsvField).join(",")).join("\r\n");
  const blob = new Blob([`﻿${csv}`], { type: "text/csv;charset=utf-8;" });
  downloadBlob(blob, filename);
}
