import { browser } from 'k6/browser';
import { check } from 'k6';

export const options = {
  scenarios: {
    ui: {
      executor: 'shared-iterations',
      options: {
        browser: {
          type: 'chromium',
        },
      },
      vus: 1,
      iterations: 1,
      startTime: "0s",
    },
  },
  thresholds: {
    checks: ['rate==1.0'],
  },
};

export default async function () {

  const context = await browser.newContext();
  await context.grantPermissions(['clipboard-read', 'clipboard-write'], {
    origin: 'https://stg-auth.singpass.gov.sg',
  });

  const page = await context.newPage();

  try {

    await page.goto('http://localhost:3080');

    await page.click('#login', {timeout: 3000});

    await page.waitForNavigation({timeout: 10000})

    console.log(`at page ${page.url()}`)

    await page.locator('button[aria-label="Password login"]').click();

    console.log(`Password login tab clicked`);

    const username = await page.locator('#username');
    await username.type('happy-user');
    console.log(`username: ${(await username.inputValue())}`);

    const password = await page.locator('#password');
    await password.type('strong password');
    console.log(`password: ${(await password.inputValue())}`);

    const singpassLoginBtn = await page.locator('button[aria-label="Submit password for Singpass login"]')

    await singpassLoginBtn.click();

    console.log(`SingPAss login button clicked`);

    //await Promise.all([page.waitForNavigation(), await page.locator('button[id="login"]').click()]);
    

    // await check(page.locator('h2'), {
    //   header: async (h2) => (await h2.textContent()) == 'Welcome, admin!',
    // });
  } finally {
    await page.close();
  }
}
