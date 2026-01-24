import { useEffect, useMemo, useState } from 'react';

const slogans = [
  'Throughput worthy of a five-year plan.',
  'Workers never sleep, even when you do.',
  'Queues that outlast winter nights.',
  'Metrics for every brigade leader.',
  'Reliability forged in steel and code.'
];

export default function StatusTicker() {
  const ordered = useMemo(() => slogans, []);
  const [index, setIndex] = useState(0);

  useEffect(() => {
    const id = window.setInterval(() => {
      setIndex((current) => (current + 1) % ordered.length);
    }, 2600);
    return () => window.clearInterval(id);
  }, [ordered.length]);

  return (
    <div className="flex items-center gap-3 rounded-lg border border-border bg-surface/70 px-4 py-3 text-sm shadow-[0_10px_40px_rgba(0,0,0,0.35)]">
      <div className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-accent">
        ★
      </div>
      <p className="flex-1 font-medium text-ink/90 transition-all">{ordered[index]}</p>
    </div>
  );
}
