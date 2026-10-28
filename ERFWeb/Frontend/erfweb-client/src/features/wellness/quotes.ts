/**
 * Healthy-living quotes and Bible verses for the Healthy living page and the daily quote dialog. Static and
 * client-only: nothing is stored or sent to the API. One a day, the same for everyone on that calendar day.
 */
export interface DailyQuote {
  text: string;
  /** Where it comes from (a Bible reference); absent for the healthy-living sayings. */
  source?: string;
}

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
export function localDayNumber(date: Date): number {
  return Math.floor(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) / 86_400_000);
}

/** Bible verses on health, rest, strength and peace, King James Version (public domain), quoted word for word. */
export const bibleVerses: readonly DailyQuote[] = [
  { text: 'Beloved, I wish above all things that thou mayest prosper and be in health, even as thy soul prospereth.', source: '3 John 1:2' },
  { text: 'What? know ye not that your body is the temple of the Holy Ghost which is in you, which ye have of God, and ye are not your own?', source: '1 Corinthians 6:19' },
  { text: 'A merry heart doeth good like a medicine: but a broken spirit drieth the bones.', source: 'Proverbs 17:22' },
  { text: 'But they that wait upon the LORD shall renew their strength; they shall mount up with wings as eagles; they shall run, and not be weary; and they shall walk, and not faint.', source: 'Isaiah 40:31' },
  { text: 'Come unto me, all ye that labour and are heavy laden, and I will give you rest.', source: 'Matthew 11:28' },
  { text: 'I can do all things through Christ which strengtheneth me.', source: 'Philippians 4:13' },
  { text: 'This is the day which the LORD hath made; we will rejoice and be glad in it.', source: 'Psalm 118:24' },
  { text: 'Pleasant words are as an honeycomb, sweet to the soul, and health to the bones.', source: 'Proverbs 16:24' },
  { text: 'Whether therefore ye eat, or drink, or whatsoever ye do, do all to the glory of God.', source: '1 Corinthians 10:31' },
  { text: 'Peace I leave with you, my peace I give unto you: not as the world giveth, give I unto you. Let not your heart be troubled, neither let it be afraid.', source: 'John 14:27' },
  { text: 'Casting all your care upon him; for he careth for you.', source: '1 Peter 5:7' },
  { text: 'Keep thy heart with all diligence; for out of it are the issues of life.', source: 'Proverbs 4:23' },
  { text: 'And let us not be weary in well doing: for in due season we shall reap, if we faint not.', source: 'Galatians 6:9' },
  { text: 'They are new every morning: great is thy faithfulness.', source: 'Lamentations 3:23' },
  { text: 'Fear thou not; for I am with thee: be not dismayed; for I am thy God: I will strengthen thee; yea, I will help thee; yea, I will uphold thee with the right hand of my righteousness.', source: 'Isaiah 41:10' },
  { text: 'Be strong and of a good courage; be not afraid, neither be thou dismayed: for the LORD thy God is with thee whithersoever thou goest.', source: 'Joshua 1:9' },
  { text: 'It shall be health to thy navel, and marrow to thy bones.', source: 'Proverbs 3:8' },
  { text: 'Take therefore no thought for the morrow: for the morrow shall take thought for the things of itself.', source: 'Matthew 6:34' },
];

/**
 * The quote for the given day: a healthy-living saying and a Bible verse on alternate days, so consecutive
 * days always differ.
 */
export function quoteForDate(date: Date): DailyQuote {
  const day = localDayNumber(date);
  const turn = Math.floor(day / 2);
  return day % 2 === 0
    ? { text: healthyQuotes[turn % healthyQuotes.length]! }
    : bibleVerses[turn % bibleVerses.length]!;
}
