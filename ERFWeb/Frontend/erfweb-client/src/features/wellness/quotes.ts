/**
 * Healthy-living quotes for the Healthy living page. Static and client-only: nothing is stored or
 * sent to the API. One quote a day, the same for everyone on that calendar day.
 */
export const healthyQuotes: readonly string[] = [
  'Take care of your body. It is the only place you have to live in.',
  'A glass of water before every meal is the easiest habit you will ever keep.',
  'Small steps every day add up to big changes in a year.',
  'Rest is not a reward for finishing work; it is part of doing it well.',
  'Eat the colours of the rainbow: the more colour on your plate, the better.',
  'A ten-minute walk after lunch does more for your afternoon than a third cup of coffee.',
  'Sleep is the best meeting you will attend all day. Do not skip it.',
  'Your health is an investment, not an expense.',
  'Stand up, stretch and look away from the screen once every hour.',
  'Cook at home when you can; you know exactly what goes into it.',
  'Laughter is light exercise for the heart.',
  'Progress, not perfection: one healthy choice today is enough.',
  'On the road? Pack fruit and water before you leave, not after you are hungry.',
  'Breathe in for four, hold for four, out for four. Calm is always one breath away.',
  'Move your body in a way you enjoy, and you will keep doing it.',
  'Less salt, less sugar, more vegetables: the simplest diet advice that works.',
  'A healthy mind needs time offline. Put the phone down at dinner.',
  'Take the stairs. Every flight is a small win.',
  'Drink water first; you are often thirsty, not hungry.',
  'Check in on a friend today. Good company is good medicine.',
  'Sunlight in the morning helps you sleep better at night.',
  'Slow down and enjoy your food; your stomach needs time to tell you it is full.',
  'Health is not about the weight you lose but the life you gain.',
  'Say no to one thing today that drains you, and yes to one that restores you.',
  'Regular check-ups catch small problems before they become big ones.',
  'Fresh air and a short walk can clear a problem a long meeting could not.',
  'Be as kind to yourself as you are to the people you care about.',
  'Prepare tomorrow\'s healthy lunch tonight, and tomorrow is already easier.',
  'Every healthy habit starts with the decision to try once.',
  'Wash your hands often; it is the cheapest medicine there is.',
  'A strong body carries a busy mind. Feed both well.',
];

/** Day number since the epoch in the viewer's local calendar, so the quote changes at local midnight. */
function localDayNumber(date: Date): number {
  return Math.floor(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) / 86_400_000);
}

/** The quote for the given day; consecutive days always get different quotes. */
export function quoteForDate(date: Date): string {
  return healthyQuotes[localDayNumber(date) % healthyQuotes.length]!;
}
