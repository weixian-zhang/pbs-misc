import { exec } from 'child_process';
import { browser } from 'k6/browser';
import { check } from 'https://jslib.k6.io/k6-utils/1.5.0/index.js';

export const options = {
  scenarios: {
    ui: {
      executor: 'shared-iterations',
      options: {
        browser: {
          type: 'chromium',
        },
      },
    },
  },
  thresholds: {
    checks: ['rate==1.0'],
  },
};

export async function createBrowser () {
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    await page.goto('https://test.k6.io/my_messages.php');

    await Promise.all([page.waitForNavigation(), await page.locator('button[id="login"]').click()]);
    //await page.locator('input[name="password"]').type('123');

    //await Promise.all([page.waitForNavigation(), page.locator('input[type="submit"]').click()]);

    await check(page.locator('h2'), {
      header: async (h2) => (await h2.textContent()) == 'Welcome, admin!',
    });
  } finally {
    await page.close();
  }
}


export function run_singpass_client() {
    exec('node start', (err, stdout, stderr) => {
        if (err) {
          // node couldn't execute the command
          return;
        }
      
        // the *entire* stdout and stderr (buffered)
        console.log(`stdout: ${stdout}`);
        console.log(`stderr: ${stderr}`);
      });
}
