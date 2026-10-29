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

/**
 * Bible verses on health, rest, strength and peace, in modern English: World English Bible, British Edition
 * (public domain), which keeps "the LORD". Word for word; Joshua 1:9, Matthew 6:34 and 1 Corinthians 6:19 are
 * shortened to read on their own.
 */
export const bibleVerses: readonly DailyQuote[] = [
  { text: 'Beloved, I pray that you may prosper in all things and be healthy, even as your soul prospers.', source: '3 John 1:2' },
  { text: 'Don\'t you know that your body is a temple of the Holy Spirit who is in you, whom you have from God?', source: '1 Corinthians 6:19' },
  { text: 'A cheerful heart makes good medicine, but a crushed spirit dries up the bones.', source: 'Proverbs 17:22' },
  { text: 'But those who wait for the LORD will renew their strength. They will mount up with wings like eagles. They will run, and not be weary. They will walk, and not faint.', source: 'Isaiah 40:31' },
  { text: 'Come to me, all you who labour and are heavily burdened, and I will give you rest.', source: 'Matthew 11:28' },
  { text: 'I can do all things through Christ who strengthens me.', source: 'Philippians 4:13' },
  { text: 'This is the day that the LORD has made. We will rejoice and be glad in it!', source: 'Psalm 118:24' },
  { text: 'Pleasant words are a honeycomb, sweet to the soul, and health to the bones.', source: 'Proverbs 16:24' },
  { text: 'Whether therefore you eat or drink, or whatever you do, do all to the glory of God.', source: '1 Corinthians 10:31' },
  { text: 'Peace I leave with you. My peace I give to you; not as the world gives, I give to you. Don\'t let your heart be troubled, neither let it be fearful.', source: 'John 14:27' },
  { text: 'Casting all your worries on him, because he cares for you.', source: '1 Peter 5:7' },
  { text: 'Keep your heart with all diligence, for out of it is the wellspring of life.', source: 'Proverbs 4:23' },
  { text: 'Let\'s not be weary in doing good, for we will reap in due season if we don\'t give up.', source: 'Galatians 6:9' },
  { text: 'They are new every morning. Great is your faithfulness.', source: 'Lamentations 3:23' },
  { text: 'Don\'t you be afraid, for I am with you. Don\'t be dismayed, for I am your God. I will strengthen you. Yes, I will help you. Yes, I will uphold you with the right hand of my righteousness.', source: 'Isaiah 41:10' },
  { text: 'Be strong and courageous. Don\'t be afraid. Don\'t be dismayed, for the LORD your God is with you wherever you go.', source: 'Joshua 1:9' },
  { text: 'It will be health to your body, and nourishment to your bones.', source: 'Proverbs 3:8' },
  { text: 'Therefore don\'t be anxious for tomorrow, for tomorrow will be anxious for itself.', source: 'Matthew 6:34' },
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
